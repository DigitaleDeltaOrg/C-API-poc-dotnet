using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shared;
using Shared.LogicalModels.C_API;
using Shared.LogicalModels.C_API.Parsing;
using Shared.LogicalModels.MeasurementResponse;
using Shared.LogicalModels.Source;
using StorageShared;

namespace Connector.Services;

public class RequestService
{
	#region Configuration
	private readonly IStorageProvider _storageProvider;
	private readonly List<string>     _supportedParameters = new() { "compartment", "measurementobject", "unit", "quantity", "quality", "parameter", "measurementdate", "value" };

	#endregion

	public RequestService(IStorageProvider storageProvider)
	{
		_storageProvider = storageProvider;
	}

	/// <summary>
	/// Process the request by converting the query to information that the source understands. Then retrieve from the source and convert it to the C-API format
	/// </summary>
	/// <param name="id"></param>
	/// <param name="sourceCodes"></param>
	/// <param name="request"></param>
	/// <param name="connectorCapabilities"></param>
	/// <returns></returns>
	public async Task<(ErrorResponse? ErrorResponse, object Content)> ProcessAsync(Guid id, List<string> sourceCodes, string request, List<ConnectorCapability> connectorCapabilities)
	{
		var parser           = new FilterParser();
		var errorResponse    = new ErrorResponse();
			
		parser.Parse(request, false, connectorCapabilities);
		if (!parser.IsValid())
		{
			errorResponse.RequestError = parser.GetErrors();
		}
				
		foreach (var condition in parser.GetConditions().Where(condition => !_supportedParameters.Contains(condition.FieldName)))
		{
			errorResponse.RequestError.Add(new ParserError(ErrorType.UnknownField, condition.FieldName));
		}

		foreach (var condition in parser.GetConditions().Where(condition => !_supportedParameters.Contains(condition.FieldName)))
		{
			errorResponse.RequestError.Add(new ParserError(ErrorType.UnknownField, condition.FieldName));
		}
		
		var invalidSources = sourceCodes.Except((await _storageProvider.GetSourcesDefinitionsAsync().ConfigureAwait(false)).Select(a => a.Code)).ToList();
		if (!sourceCodes.Any())
		{
			errorResponse.SourceErrors.Add(new SourceError() { Error = SourceErrorType.Missing, Source = string.Empty});
		}
		
		if (invalidSources.Any())
		{
			errorResponse.SourceErrors.AddRange(invalidSources.Select(a => new SourceError() { Error = SourceErrorType.Unknown, Source = a}).ToList());
		}
		
		var duplicates =  sourceCodes.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
		if (duplicates.Any())
		{
			errorResponse.SourceErrors.AddRange(invalidSources.Select(a => new SourceError() { Error = SourceErrorType.Duplicate, Source = a}).ToList());
		}
		
		if (errorResponse.RequestError.Any() || errorResponse.SourceErrors.Any())
		{
			return (errorResponse, string.Empty);
		}

		var processes = await DetermineProcesses(sourceCodes);
		await Task.WhenAll(processes.Select(a => RequestDataForSourceAsync(id, a.definition, connectorCapabilities, a.address, parser))).ConfigureAwait(true); // Run the query tasks in parallel.
		var measurements = await _storageProvider.GetMeasurementsOfResponseCachesForIdAsync(id).ConfigureAwait(false);

		await _storageProvider.CleanupResponseCacheForIdAsync(id).ConfigureAwait(false);
		return (null, measurements);
	}

	/// <summary>
	/// Request data from the source
	/// </summary>
	/// <param name="id">Id obtained from the Connector</param>
	/// <param name="sourceDefinition">Source</param>
	/// <param name="connectorCapabilities"></param>
	/// <param name="pluginUrl">Plugin url</param>
	/// <param name="parser"></param>
	private async Task RequestDataForSourceAsync(Guid id, SourceDefinition sourceDefinition, List<ConnectorCapability> connectorCapabilities, string pluginUrl, FilterParser parser)
	{
		var client = new HttpClient();
		client.Timeout = TimeSpan.FromMinutes(3);

		try
		{
			var queryResponse = await client.PostAsJsonAsync(new Uri($"{pluginUrl}/measurements"), new DataBody { SourceDefinition = sourceDefinition, Conditions = parser.GetConditions(), ResponseId = id, ConnectorCapabilities = connectorCapabilities}).ConfigureAwait(false);
			if (queryResponse.StatusCode == HttpStatusCode.OK)
			{
				var result = JsonConvert.DeserializeObject<MeasurementResponse>(await queryResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
				if (result != null)
				{
					await _storageProvider.AddCachedResponseForIdAsync(id, result).ConfigureAwait(false);
					return;
				}
			}

			var errorResult = new MeasurementResponse
			{
				HttpStatus = (int)queryResponse.StatusCode
			};
			await _storageProvider.AddCachedResponseForIdAsync(id, errorResult).ConfigureAwait(false);
		}
		catch (Exception)
		{
			var errorResult = new MeasurementResponse
			{
				HttpStatus = (int)HttpStatusCode.BadGateway,
				Source = sourceDefinition.Code,
				RequestId = id
			};
			await _storageProvider.AddCachedResponseForIdAsync(id, errorResult).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Validate the sources
	/// </summary>
	/// <param name="sources"></param>
	/// <returns>List of errors</returns>
	public async Task<List<SourceError>> ValidateSourcesAsync(string sources)
	{
		var sourceErrors = new List<SourceError>();
		if (string.IsNullOrWhiteSpace(sources))
		{
			sourceErrors.Add(new SourceError { Error = SourceErrorType.Unknown});
			return sourceErrors;
		}
			
		var sourceList = sources.Split(",");
		var duplicates = sourceList.GroupBy(a => a).Where(a => a.Count() > 1).Select(a => a.Key).ToList();
		if (duplicates.Any())
		{
			sourceErrors.AddRange(duplicates.Select(duplicate => new SourceError { Error = SourceErrorType.Duplicate, Source = duplicate }));
		}
			
		var validSources = (await _storageProvider.GetSourcesDefinitionsAsync().ConfigureAwait(false)).Select(a => a.Code);
		var unknowns     = sourceList.Except(validSources).ToList();
		if (!unknowns.Any())
		{
			return sourceErrors;
		}

		sourceErrors.AddRange(unknowns.Select(unknown => new SourceError { Error = SourceErrorType.Unknown, Source = unknown }));

		return sourceErrors;
	}
		
	/// <summary>
	/// Process the request by converting the query to information that the source understands. Then retrieve from the source and convert it to the C-API format
	/// </summary>
	/// <param name="sourceCodes"></param>
	/// <param name="entityType"></param>
	/// <returns></returns>
	public async Task<(ErrorResponse? ErrorResponse, List<SourceCodeName> Content)> GetItemsAsync(List<string> sourceCodes, string entityType)
	{
		var entities = new List<SourceCodeName>();
		var processes = await DetermineProcesses(sourceCodes);
		await Task.WhenAll(processes.Select(a => RequestListForSourceAsync(a.definition, a.address, entityType, entities))).ConfigureAwait(true); // Run the query tasks in parallel.

		return (null, entities);
	}

	/// <summary>
	/// Request data from the source. Items are in Source/Code/Name format.
	/// </summary>
	/// <param name="sourceDefinition">Source</param>
	/// <param name="pluginUrl">Plugin url</param>
	/// <param name="entityType"></param>
	/// <param name="entities"></param>
	private static async Task RequestListForSourceAsync(SourceDefinition sourceDefinition, string pluginUrl, string entityType, List<SourceCodeName> entities)
	{
		var client        = new HttpClient();
		var dataBody      = new DataBody { SourceDefinition = sourceDefinition };
		var queryResponse = await client.PostAsJsonAsync($"{pluginUrl}/{entityType}", dataBody).ConfigureAwait(false);
		var result        = JsonConvert.DeserializeObject<List<SourceCodeName>>(await queryResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
		if (result == null)
		{
			return;
		}
			
		entities.AddRange(result); 
	}
	
	/// <summary>
	/// Request measurement objects from the source.
	/// </summary>
	/// <param name="sourceDefinition">Source</param>
	/// <param name="pluginUrl">Plugin url</param>
	/// <param name="entities"></param>
	private static async Task RequestMeasurementObjectListForSourceAsync(SourceDefinition sourceDefinition, string pluginUrl, List<MeasurementObject> entities)
	{
		var client        = new HttpClient();
		// Set payload
		var dataBody      = new DataBody { SourceDefinition = sourceDefinition };
		var queryResponse = await client.PostAsJsonAsync($"{pluginUrl}/measurementobjects", dataBody).ConfigureAwait(false);
		var result        = JsonConvert.DeserializeObject<List<MeasurementObject>>(await queryResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
		if (result == null)
		{
			return;
		}
			
		entities.AddRange(result); 
	}

	/// <summary>
	/// Retrieve measurement objects from the specified sources.
	/// </summary>
	/// <param name="sourceCodes"></param>
	/// <returns></returns>
	public async Task<(ErrorResponse? ErrorResponse, List<MeasurementObject> Content)> GetMeasurementObjectsAsync(List<string> sourceCodes)
	{
		var entities  = new List<MeasurementObject>();
		var processes = await DetermineProcesses(sourceCodes);
		await Task.WhenAll(processes.Select(a => RequestMeasurementObjectListForSourceAsync(a.definition, a.address, entities))).ConfigureAwait(true); // Run the query tasks in parallel.
		
		return (null, entities);
	}

	private async Task<List<(string code, string address, SourceDefinition definition)>> DetermineProcesses(List<string> sourceCodes)
	{
		var processes = new List<(string code, string address, SourceDefinition definition)>();
		foreach (var code in sourceCodes)
		{
			var source = await _storageProvider.GetSourceByCodeAsync(code).ConfigureAwait(false);
			var plugin = await _storageProvider.GetPluginByCodeAsync(source!.Plugin).ConfigureAwait(false);
			if (plugin == null)
			{
				continue;
			}

			processes.Add((code, plugin.Url, source));
		}

		return processes;
	}
	
}