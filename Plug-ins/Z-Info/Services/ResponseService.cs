using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PluginShared;
using Shared.LogicalModels.C_API;
using Shared.LogicalModels.C_API.Parsing;
using Shared.LogicalModels.Measurement;
using Shared.LogicalModels.MeasurementResponse;
using Shared.LogicalModels.Source;

namespace Z_Info.Services;

internal static class ResponseService
{
	/// <summary>
	/// Main worker for interpreting and processing the C-API request.
	/// </summary>
	/// <param name="dataBody">Data send by POST of the HTTP request. Contains the FilterParser and the Source that the plugin needs to address.</param>
	/// <returns>ResponseCache containing status, errors and results of the request.</returns>
	public static async Task<MeasurementResponse> ProcessRequestAsync(DataBody dataBody)
	{
		var responseCache = new MeasurementResponse { RequestId = dataBody.ResponseId, Source = dataBody.SourceDefinition.Code };
		var start         = DateTime.Now;
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			responseCache.Errors.Add("Authentication failed");
			return responseCache;
		}
			
		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);
		
		var query = ConstructQuery(dataBody.Conditions, dataBody.SourceDefinition);
		await ProcessRequestAsync(query, client, responseCache, dataBody.SourceDefinition, token).ConfigureAwait(false);

		responseCache.Statistics.Source                     = dataBody.SourceDefinition.Code;
		responseCache.Statistics.ResponseTimeInMilliSeconds = (DateTime.Now - start).Milliseconds;
		responseCache.HttpStatus                            = 200;
		return responseCache;
	}

	/// <summary>
	/// Process the request.
	/// </summary>
	/// <param name="request">Query string</param>
	/// <param name="client">HTTP Client</param>
	/// <param name="measurementResponse">Response cache</param>
	/// <param name="sourceDefinition">Source</param>
	/// <param name="token">Bearer token</param>
	private static async Task ProcessRequestAsync(string request, HttpClient client, MeasurementResponse measurementResponse, SourceDefinition sourceDefinition, string? token)
	{
		if (sourceDefinition.ZInfoConfigurationSection == null)
		{
			return;
		}
		
		client.Timeout = TimeSpan.FromMinutes(1);
		try
		{
			LogonService.HandlePostLogin(sourceDefinition, client, token);

			var queryHttpResponse = await client.GetAsync($"?SPCID={sourceDefinition.ZInfoConfigurationSection.MeasurementRequest}&vraag={request}").ConfigureAwait(false);
			await HttpResponseDataToCacheResponseAsync(queryHttpResponse, measurementResponse, sourceDefinition).ConfigureAwait(false);
			measurementResponse.Statistics.NumberOfRequests++;
		}
		catch (InvalidOperationException e)
		{
			measurementResponse.Errors.Add($"Invalid operation exception: {e.Message}");
		}
		catch (HttpRequestException e)
		{
			measurementResponse.Errors.Add($"Request exception: {e.Message}");
		}
		catch (TaskCanceledException e)
		{
			measurementResponse.Errors.Add($"Task cancelled exception: {e.Message}");
		}
	}

	/// <summary>
	/// Process the response.
	/// </summary>
	/// <param name="queryHttpResponse">Response</param>
	/// <param name="measurementResponse">Container for the measurements, errors and statistics</param>
	/// <param name="sourceDefinition">C-API translations</param>
	private static async Task HttpResponseDataToCacheResponseAsync(HttpResponseMessage queryHttpResponse, MeasurementResponse measurementResponse, SourceDefinition sourceDefinition)
	{
		var content = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
		if (!queryHttpResponse.IsSuccessStatusCode)
		{
			measurementResponse.Statistics.TotalByteCount += content.Length;
			measurementResponse.HttpStatus                =  (int)queryHttpResponse.StatusCode;
			return;
		}
		
		var response     = JsonDocument.Parse(content);
		var measurements = response.RootElement.GetProperty("waarden").EnumerateArray();
		HandleMeasurements(measurementResponse, measurements, sourceDefinition);
	}

	private static void HandleMeasurements(MeasurementResponse measurementResponse, JsonElement.ArrayEnumerator measurements, SourceDefinition sourceDefinition)
	{
		try
		{
			if (!measurements.Any())
			{
				return;
			}

			foreach (var measurement in measurements)
			{
				var entity = new Measurement();
				HandleMeasurementProperties(measurement.EnumerateObject(), entity, sourceDefinition);
				entity.Source = sourceDefinition.Name;
				measurementResponse.Measurements.Add(entity);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	private static void HandleMeasurementProperties(JsonElement.ObjectEnumerator objects, Measurement entity, SourceDefinition sourceDefinition)
	{
		HandleUnits(entity, objects, sourceDefinition);
		HandleValue(entity, objects, sourceDefinition);
		HandleCompartment(entity, objects, sourceDefinition);
		HandleDate(entity, objects, sourceDefinition);
		HandleMeasurementObject(entity, objects, sourceDefinition);
		HandleParameter(entity, objects, sourceDefinition);
		HandleQuantity(entity, objects, sourceDefinition);
		HandleAdditionalProperties(entity, objects, sourceDefinition);
	}

	private static void HandleAdditionalProperties(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}

		var knownFields = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.Where(a => a.DataCategory != DataCategory.Other).SelectMany(a => a.ZInfoName).ToList();
		entity.AdditionalData ??= new();
		foreach (var @object in objects.Where(@object => !knownFields.Contains(@object.Name) && !entity.AdditionalData.ContainsKey(@object.Name)))
		{
			switch (@object.Value.ValueKind)
			{
				case JsonValueKind.Object:
					entity.AdditionalData.Add(@object.Name, JsonConvert.DeserializeObject(@object.Value.GetRawText()) ?? new object());
					break;
				case JsonValueKind.Array:
					entity.AdditionalData.Add(@object.Name, JsonConvert.DeserializeObject(@object.Value.GetRawText()) ?? new object());
					break;
				case JsonValueKind.String:
					entity.AdditionalData.Add(@object.Name, @object.Value.GetString() ?? string.Empty);
					break;
				case JsonValueKind.Number:
					entity.AdditionalData.Add(@object.Name, @object.Value.GetDecimal());
					break;
				case JsonValueKind.True:
					entity.AdditionalData.Add(@object.Name, @object.Value.GetBoolean());
					break;
				case JsonValueKind.False:
					entity.AdditionalData.Add(@object.Name, @object.Value.GetBoolean());
					break;
				default:
					entity.AdditionalData.Add(@object.Name, @object.Value.GetRawText());
					break;
			}
		}
	}

	private static void HandleQuantity(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}
	
		var knownFields = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.Where(a => a.DataCategory == DataCategory.Quantity).SelectMany(a => a.ZInfoName).ToList();
		foreach (var @object in objects)
		{
			if (knownFields.Contains(@object.Name))
			{
				entity.Quantity = @object.Value.GetString();
			}
		}
	}

	private static void HandleParameter(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}
	
		var knownFields = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.Where(a => a.DataCategory == DataCategory.Parameter).SelectMany(a => a.ZInfoName).ToList();
		foreach (var @object in objects)
		{
			if (knownFields.Contains(@object.Name))
			{
				entity.Parameter = @object.Value.GetString();
			}
		}
	}

	private static void HandleMeasurementObject(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}

		entity.MeasurementObject = ConstructMeasurementObject(objects);
	}

	private static string ConstructMeasurementObject(JsonElement.ObjectEnumerator objects)
	{
		var ist = objects.FirstOrDefault(a => a.Name == "ist").Value.GetString() ?? string.Empty;
		var dps = objects.FirstOrDefault(a => a.Name == "dps").Value.GetString() ?? string.Empty;
		return $"{ist}_{dps}";
	}

	private static void HandleDate(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.Where(a => a.DataCategory == DataCategory.MeasurementDate).SelectMany(a => a.ZInfoName).ToList();
		foreach (var @object in objects)
		{
			if (knownFields.Contains(@object.Name))
			{
				entity.MeasurementDate = JsonConvert.DeserializeObject<DateTimeOffset>(@object.Value.GetRawText());
			}
		}
	}

	private static void HandleCompartment(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.Where(a => a.DataCategory == DataCategory.Compartment).SelectMany(a => a.ZInfoName).ToList();
		foreach (var @object in objects)
		{
			if (knownFields.Contains(@object.Name))
			{
				entity.Compartment = @object.Value.GetString();
			}
		}
	}

	private static void HandleValue(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.Where(a => a.DataCategory == DataCategory.Value).SelectMany(a => a.ZInfoName).ToList();
		foreach (var @object in objects)
		{
			if (knownFields.Contains(@object.Name))
			{
				entity.Value = JsonConvert.DeserializeObject<decimal>(@object.Value.GetRawText());
			}
		}
	}

	private static void HandleUnits(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.InputParameterMap == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.Where(a => a.DataCategory == DataCategory.Unit).SelectMany(a => a.ZInfoName).ToList();
		foreach (var @object in objects)
		{
			if (knownFields.Contains(@object.Name))
			{
				entity.Unit = @object.Value.GetString();
			}
		}
	}
	
	/// <summary>
	/// Define the query. Unsupported items will be handled in the post-query filtering.
	/// </summary>
	/// <param name="filter">Specified filter from the connector.</param>
	/// <param name="sourceDefinition"></param>
	/// <returns>Constructed query for Z-Info.</returns>
	private static string ConstructQuery(List<Condition>? filter, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.ZInfoConfigurationSection?.ZInfoRequiredQueryParameters == null || filter == null)
		{
			return string.Empty;
		}
		
		// Pre-fill the query based on the definitions of the required fields from the source with 'allow all'.
		var queryParameters = sourceDefinition.ZInfoConfigurationSection.ZInfoRequiredQueryParameters.ToDictionary(a => a, _ => "*");
		foreach (var item in filter)
		{
			switch (item.FieldName)
			{
				case "compartment":
					HandleQueryPart(item, DataCategory.Compartment, queryParameters, sourceDefinition);
					break;
				case "parameter":
					HandleQueryPart(item, DataCategory.Parameter, queryParameters, sourceDefinition);
					break;
				case "quantity":
					HandleQueryPart(item, DataCategory.Quantity, queryParameters, sourceDefinition);
					break;
				case "unit":
					HandleQueryPart(item, DataCategory.Unit, queryParameters, sourceDefinition);
					break;
			}
		}

		var rewrittenFilter = RewriteFiltersForMeasurementObjectQueries(filter);
		HandleDateQueryPart(rewrittenFilter, queryParameters, sourceDefinition.ZInfoConfigurationSection.StartDateParameter, sourceDefinition.ZInfoConfigurationSection.EndDateParameter); // Can be specified multiple times to define a range.
		return string.Join(";", queryParameters.Select(a => $"${a.Key}$={a.Value}"));
	}

	private static IEnumerable<Condition> RewriteFiltersForMeasurementObjectQueries(List<Condition> filter)
	{
		var rewrittenFilter = new List<Condition>();
		rewrittenFilter.AddRange(filter.Where(a => a.FieldName != "measurementobject"));
		foreach (var filterOfInterest in filter.Where(a => a.FieldName == "measurementobject"))
		{
			switch (filterOfInterest.CompareMethod)
			{
				case CompareMethod.Eq:
				{
					if (!filterOfInterest.CompareDataAsString().Contains('_'))
					{
						continue;
					}

					var ist = filterOfInterest.CompareDataAsString().Split("_")[0];
					var dps = filterOfInterest.CompareDataAsString().Split("_")[1];
					rewrittenFilter.Add(new Condition("ist", CompareMethod.Eq, DataType.StringType, ist));
					rewrittenFilter.Add(new Condition("dps", CompareMethod.Eq, DataType.StringType, dps));
					break;
				}
				case CompareMethod.Ne:
				{
					var ist = filterOfInterest.CompareDataAsString().Split("_")[0];
					var dps = filterOfInterest.CompareDataAsString().Split("_")[1];
					rewrittenFilter.Add(new Condition("ist", CompareMethod.Ne, DataType.StringType, ist));
					rewrittenFilter.Add(new Condition("dps", CompareMethod.Ne, DataType.StringType, dps));
					break;
				}
				case CompareMethod.In:
					foreach (var value in filterOfInterest.CompareDataAsArrayOfString().Where(a => a.Contains('_')))
					{
						var ist = value.Split("_")[0];
						var dps = value.Split("_")[1];
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Eq, DataType.StringType, ist));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Eq, DataType.StringType, dps));
					}
					break;
				case CompareMethod.Notin:
					foreach (var value in filterOfInterest.CompareDataAsArrayOfString().Where(a => a.Contains('_')))
					{
						var ist = value.Split("_")[0];
						var dps = value.Split("_")[1];
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Ne, DataType.StringType, ist));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Ne, DataType.StringType, dps));
					}
					break;
				case CompareMethod.Like:
				{
					if (filterOfInterest.CompareDataAsString().Contains('_'))
					{
						var ist = filterOfInterest.CompareDataAsString().Split("_")[0];
						var dps = filterOfInterest.CompareDataAsString().Split("_")[1];
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Endswith, DataType.StringType, ist));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Startswith, DataType.StringType, dps));
					}
					else
					{
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Like, DataType.StringType, filterOfInterest.CompareDataAsString()));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Like, DataType.StringType, filterOfInterest.CompareDataAsString()));
					}
					break;
				}
				case CompareMethod.Startswith:
					if (filterOfInterest.CompareDataAsString().Contains('_'))
					{
						var ist = filterOfInterest.CompareDataAsString().Split("_")[0];
						var dps = filterOfInterest.CompareDataAsString().Split("_")[1];
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Eq, DataType.StringType, ist));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Startswith, DataType.StringType, dps));
					}
					else
					{
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Startswith, DataType.StringType, filterOfInterest.CompareDataAsString()));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Startswith, DataType.StringType, filterOfInterest.CompareDataAsString()));
					}
					break;
				case CompareMethod.Endswith:
					if (filterOfInterest.CompareDataAsString().Contains('_'))
					{
						var ist = filterOfInterest.CompareDataAsString().Split("_")[0];
						var dps = filterOfInterest.CompareDataAsString().Split("_")[1];
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Startswith, DataType.StringType, ist));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Eq, DataType.StringType, dps));
					}
					else
					{
						rewrittenFilter.Add(new Condition("ist", CompareMethod.Endswith, DataType.StringType, filterOfInterest.CompareDataAsString()));
						rewrittenFilter.Add(new Condition("dps", CompareMethod.Endswith, DataType.StringType, filterOfInterest.CompareDataAsString()));
					}
					break;
			}
		}
		
		return rewrittenFilter;
	}

	private static void HandleDateQueryPart(IEnumerable<Condition>? conditions, IDictionary<string, string> queryParameters, string startDateParameter, string endDateParameter)
	{
		var dateParameters = conditions?.Where(a => a.FieldName == "measurementdate").ToList();
		if (!(dateParameters?.Any() ?? false))
		{
			return;
		}

		foreach (var dateParameter in dateParameters)
		{
			switch (dateParameter.CompareMethod)
			{
				case (CompareMethod.Eq):
					queryParameters[startDateParameter] = dateParameter.CompareDataAsDateTimeOffset().Date.ToString("yyyy-MM-dd");
					queryParameters[endDateParameter]   = dateParameter.CompareDataAsDateTimeOffset().Date.ToString("yyyy-MM-dd");
					break;
				case (CompareMethod.Lt):
					queryParameters[endDateParameter] = dateParameter.CompareDataAsDateTimeOffset().Date.ToString("yyyy-MM-dd");
					break;
				case (CompareMethod.Le):
					queryParameters[endDateParameter] = dateParameter.CompareDataAsDateTimeOffset().Date.ToString("yyyy-MM-dd");
					break;
				case (CompareMethod.Ge):
					queryParameters[startDateParameter] = dateParameter.CompareDataAsDateTimeOffset().Date.ToString("yyyy-MM-dd");
					break;
				case (CompareMethod.Gt):
					queryParameters[startDateParameter] = dateParameter.CompareDataAsDateTimeOffset().Date.ToString("yyyy-MM-dd");
					break;
			}
		}
	}

	private static void HandleQueryPart(Condition item, DataCategory dataCategory, IDictionary<string, string> queryParameters, SourceDefinition sourceDefinition)
	{
		if (sourceDefinition.MapData == null || sourceDefinition.MapData.All(a => a.CApiDataCategory != dataCategory) || sourceDefinition.ZInfoConfigurationSection == null)
		{
			return;
		}
		
		var cApiName       = sourceDefinition.MapData.FirstOrDefault(a => a.CApiDataCategory == dataCategory)?.CapiName;
		if (cApiName == null)
		{
			return;
		}
		
		var zInfoParameter = sourceDefinition.ZInfoConfigurationSection.InputParameterMap.First(a => a.CapiName == cApiName).ZInfoName.FirstOrDefault();
		if (zInfoParameter == null)
		{
			return;
		}

		queryParameters[zInfoParameter] = item.CompareMethod switch
		{
			CompareMethod.Endswith => $"*{item.CompareData}",
			CompareMethod.Eq => $"{item.CompareData}",
			CompareMethod.In => string.Join("|", item.CompareDataAsArrayOfString()),
			CompareMethod.Like => $"*{item.CompareData}*",
			CompareMethod.Startswith => $"{item.CompareData}*",
			_ => queryParameters[zInfoParameter]
		};
	}
	
	/// <summary>
	/// Query compartments.
	/// </summary>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetCompartmentsAsync(DataBody dataBody)
	{
		if (dataBody.SourceDefinition.ZInfoConfigurationSection?.CompartmentRequest == null)
		{
			return (200, new List<SourceCodeName>());
		}
		
		return await GetEntitiesAsync(dataBody.SourceDefinition.ZInfoConfigurationSection.CompartmentRequest, dataBody.SourceDefinition).ConfigureAwait(false);
	}
		
	/// <summary>
	/// Query units.
	/// </summary>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetUnitsAsync(DataBody dataBody)
	{
		if (dataBody.SourceDefinition.ZInfoConfigurationSection?.UnitRequest == null)
		{
			return (200, new List<SourceCodeName>());
		}
		
		return await GetEntitiesAsync(dataBody.SourceDefinition.ZInfoConfigurationSection.UnitRequest, dataBody.SourceDefinition).ConfigureAwait(false);
	}
		
	/// <summary>
	/// Query quantities.
	/// </summary>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetQuantitiesAsync(DataBody dataBody)
	{
		if (dataBody.SourceDefinition.ZInfoConfigurationSection?.QuantityRequest == null)
		{
			return (200, new List<SourceCodeName>());
		}
		
		return await GetEntitiesAsync(dataBody.SourceDefinition.ZInfoConfigurationSection.QuantityRequest, dataBody.SourceDefinition).ConfigureAwait(false);
	}
		
	/// <summary>
	/// Query parameters.
	/// </summary>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetParametersAsync(DataBody dataBody)
	{
		if (dataBody.SourceDefinition.ZInfoConfigurationSection?.ParameterRequest == null)
		{
			return (200, new List<SourceCodeName>());
		}
		
		return await GetEntitiesAsync(dataBody.SourceDefinition.ZInfoConfigurationSection.ParameterRequest, dataBody.SourceDefinition).ConfigureAwait(false);
	}

	private static async Task<(int StatusCode, List<SourceCodeName> Entities)> GetEntitiesAsync(EndPointFieldNameDescriptionName request, SourceDefinition sourceDefinition)
	{
		var (statusCode, token) = await LogonService.AuthenticateAsync(sourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, new List<SourceCodeName>());
		}
		
		var client = new HttpClient { BaseAddress = new Uri(sourceDefinition.Url) };
		LogonService.HandlePostLogin(sourceDefinition, client, token);

		client.Timeout = TimeSpan.FromMinutes(1);
		var entities = new List<SourceCodeName>();
		try
		{
			var queryHttpResponse = await client.GetAsync($"?SPCID={request.EndPoint}").ConfigureAwait(false);
			var content           = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
			if (!queryHttpResponse.IsSuccessStatusCode)
			{
				return ((int)queryHttpResponse.StatusCode, new List<SourceCodeName>());
			}

			var response = JsonDocument.Parse(content);
			var values   = response.RootElement.GetProperty("waarden").EnumerateArray();
			entities.AddRange(values.Select(value => new SourceCodeName { Source = sourceDefinition.Code, Code = value.GetProperty(request.FieldName).ToString(), Name = value.GetProperty(request.DescriptionName).ToString()}));
			return ((int)queryHttpResponse.StatusCode, entities);
		}
		catch (Exception)
		{
			return ((int)HttpStatusCode.InternalServerError, new List<SourceCodeName>());
		}
	}

	public static async Task<(int HttpStatus, List<MeasurementObject> Entities)> GetMeasurementObjectsAsync(DataBody dataBody)
	{
		if (dataBody.SourceDefinition.ZInfoConfigurationSection?.MeasurementObjectRequest == null)
		{
			return ((int)HttpStatusCode.InternalServerError, new List<MeasurementObject>());
		}
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, new List<MeasurementObject>());
		}
		
		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);

		client.Timeout = TimeSpan.FromMinutes(1);
		var entities = new List<MeasurementObject>();
		try
		{
			var queryHttpResponse = await client.GetAsync($"?SPCID={dataBody.SourceDefinition.ZInfoConfigurationSection.MeasurementObjectRequest.EndPoint}").ConfigureAwait(false);
			var content           = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
			if (!queryHttpResponse.IsSuccessStatusCode)
			{
				return ((int)queryHttpResponse.StatusCode, new List<MeasurementObject>());
			}
			
			var response = JsonDocument.Parse(content);
			var values   = response.RootElement.GetProperty("waarden").EnumerateArray();
			entities.AddRange(values.Select(value => new MeasurementObject { Source = dataBody.SourceDefinition.Code, Code = $"{value.GetProperty("dps").GetString()}_{value.GetProperty("ist").GetString()}", Name = $"{value.GetProperty("dps").GetString()}_{value.GetProperty("ist").GetString()}_{value.GetProperty("dpsOmschr").GetString()}"}));
			return ((int)queryHttpResponse.StatusCode, entities);
		}
		catch (Exception)
		{
			return ((int)HttpStatusCode.InternalServerError, new List<MeasurementObject>());
		}
	}
}
