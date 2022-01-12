using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BAMCIS.GeoJSON;
using DD_API.Models;
using Newtonsoft.Json;
using PluginShared;
using PluginShared.Helpers;
using Shared.LogicalModels;
using Shared.LogicalModels.C_API;
using Shared.LogicalModels.C_API.Parsing;
using Shared.LogicalModels.Measurement;
using Shared.LogicalModels.MeasurementResponse;
using Shared.LogicalModels.Source;
using ErrorResponse = DD_API.Models.ErrorResponse;
using Newtonsoft.Json.Serialization;

namespace DD_API.Services;

/// <summary>
///   The DD-API V2 specification (v2) is strictly defined, as the data it deals with, doesn't change much. The filter
///   parameters are limited and fixed. There is no wild-card support, for instance.
///   This service converts C-API V2 queries to DD-API V2 queries and works around some limitations:
///   - implement a filter (filtered *after* the query took place) for compartments.
///   - implement non-exact match filters as post query filters.
///   - implement measurementobject:in:, parameter:in:, quantity:in: filter parts as separate exact-match queries.
///   Post-processing (removing measurements that do not match the filters that are not supported by the DD-API) is
///   performed after a query result is retrieved from the server, to reduce memory consumption.
/// </summary>
public static class ResponseService
{
		
	#region public
	/// <summary>
	///   Main worker for interpreting and processing the C-API request.
	/// </summary>
	/// <param name="dataBody">
	///   Data send by POST of the HTTP request. Contains the conditions and the Source that the plugin
	///   needs to address.
	/// </param>
	/// <returns>ResponseCache containing status, errors and results of the request.</returns>
	public static async Task<MeasurementResponse> ProcessRequestAsync(DataBody dataBody)
	{
		var responseCache = new MeasurementResponse { RequestId = dataBody.ResponseId, Source = dataBody.SourceDefinition.Code };
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
			
		if (statusCode != HttpStatusCode.OK)
		{
			responseCache.Errors.Add("Authentication failed");
			return responseCache;
		}
			
		var client = new HttpClient { BaseAddress = new(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);

		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);
		var baseRequest = ComposeRequest(dataBody.SourceDefinition.MapData!, dataBody.Conditions ?? new List<Condition>(), KnownDdApiFiltersAndFilterNames.Select(a => a.Key), dataBody.SourceDefinition.SupportedQueryParameters, dataBody.ConnectorCapabilities);
		baseRequest.Append(AddMeasurementDateToRequest(dataBody.Conditions!, dataBody.ConnectorCapabilities));
		var requests = ConstructQueriesForKnownInOperatorsKnownByDdApi(dataBody.SourceDefinition.MapData ?? new List<Map>(), dataBody.Conditions, KnownDdApiFiltersAndFilterNames, dataBody.SourceDefinition.SupportedQueryParameters ?? new List<string>(), baseRequest.ToString(), dataBody.ConnectorCapabilities);
		foreach (var request in requests)
		{
			await ProcessRequestAsync(dataBody.SourceDefinition.MapData ?? new List<Map>(), dataBody, client, responseCache, request).ConfigureAwait(false);
		}

		responseCache.Statistics.Source = dataBody.SourceDefinition.Code;
		responseCache.HttpStatus        = 200;
		return responseCache;
	}
	
	/// <summary>
	/// Get a list of parameters.
	/// </summary>
	/// <param name="dataBody">DataBody as provided by the source</param>
	/// <returns>Combination of HttpStatus code and a list of parameters</returns>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetParametersAsync(DataBody dataBody)
	{
		var (httpStatus, observationTypes) = await GetObservationTypesAsync(dataBody).ConfigureAwait(false);
		if (httpStatus != HttpStatusCode.OK)
		{
			return ((int)httpStatus, new());
		}

		var items = observationTypes.Where(a => a.Value == DataCategory.Parameter).Select(a => new SourceCodeName { Source = dataBody.SourceDefinition.Code, Code = a.Key, Name = a.Key }).ToList();
		return ((int)HttpStatusCode.OK, items);
	}

	/// <summary>
	/// Get a list of compartments.
	/// </summary>
	/// <param name="dataBody">DataBody as provided by the source</param>
	/// <returns>Combination of HttpStatus code and a list of compartments</returns>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetCompartmentsAsync(DataBody dataBody)
	{
		var (httpStatus, observationTypes) = await GetObservationTypesAsync(dataBody).ConfigureAwait(false);
		if (httpStatus != HttpStatusCode.OK)
		{
			return ((int)httpStatus, new());
		}

		var items = observationTypes.Where(a => a.Value == DataCategory.Compartment).Select(a => new SourceCodeName { Source = dataBody.SourceDefinition.Code, Code = a.Key, Name = a.Key }).ToList();
		return ((int)HttpStatusCode.OK, items);
	}
		
	/// <summary>
	/// Get a list of units.
	/// </summary>
	/// <param name="dataBody">DataBody as provided by the source</param>
	/// <returns>Combination of HttpStatus code and a list of units.</returns>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetUnitsAsync(DataBody dataBody)
	{
		var (httpStatus, observationTypes) = await GetObservationTypesAsync(dataBody).ConfigureAwait(false);
		if (httpStatus != HttpStatusCode.OK)
		{
			return ((int)httpStatus, new());
		}

		var items = observationTypes.Where(a => a.Value == DataCategory.Unit).Select(a => new SourceCodeName { Source = dataBody.SourceDefinition.Code, Code = a.Key, Name = a.Key }).ToList();
		return ((int)HttpStatusCode.OK, items);
	}

	/// <summary>
	/// Get a list of quantities.
	/// </summary>
	/// <param name="dataBody">DataBody as provided by the source</param>
	/// <returns>Combination of HttpStatus code and a list of quantities.</returns>
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetQuantitiesAsync(DataBody dataBody)
	{
		var (httpStatus, observationTypes) = await GetObservationTypesAsync(dataBody).ConfigureAwait(false);
		if (httpStatus != HttpStatusCode.OK)
		{
			return ((int)httpStatus, new());
		}

		var items = observationTypes.Where(a => a.Value == DataCategory.Quantity).Select(a => new SourceCodeName { Source = dataBody.SourceDefinition.Code, Code = a.Key, Name = a.Key }).ToList();
		return ((int)HttpStatusCode.OK, items);
	}
	#endregion
		
	/// <summary>
	/// Maps C-API standard query parameters to the standard DD-API equivalents.
	/// </summary>
	private static readonly Dictionary<string, string> KnownDdApiFiltersAndFilterNames = new()
	{
		{ "measurementobject", "locationCode" },
		{ "parameter", "parameterCode" },
		{ "quantity", "quantityName" },
		{ "unit", "unit"}
	};


	/// <summary>
	///   Filter measurements on location names.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="measurements">Measurements to be filtered.</param>
	/// <param name="connectorCapabilities"></param>
	private static void FilterLocationNames(IEnumerable<Condition>? conditions, List<Measurement> measurements, List<ConnectorCapability> connectorCapabilities)
	{	
		if (conditions == null)
		{
			return;
		}

		var filteredConditions = conditions.Where(a => a.FieldName == ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.MeasurementObject)).ToList();
		if (!filteredConditions.Any())
		{
			return;
		}

		foreach (var condition in filteredConditions)
		{
			switch (condition.CompareMethod)
			{
				case (CompareMethod.Ne):
					measurements.RemoveAll(a => (a.MeasurementObject ?? "") != condition.CompareDataAsString());
					break;
				case (CompareMethod.Like):
					measurements.RemoveAll(a => !(a.MeasurementObject ?? "").Contains(condition.CompareDataAsString()));
					break;
				case (CompareMethod.Startswith):
					measurements.RemoveAll(a => !(a.MeasurementObject ?? "").StartsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.Endswith):
					measurements.RemoveAll(a => !(a.MeasurementObject ?? "").EndsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.In):
					measurements.RemoveAll(a => !condition.CompareDataAsArrayOfString().Contains(a.MeasurementObject ?? ""));
					break;
				case (CompareMethod.Bbox):
					measurements.RemoveAll(a => !InBoundingBox(GeoJsonToDecimalArray(a.Coordinate), condition.CompareDataAsBoundingBox()));
					break;
			}
		}
	}

	/// <summary>
	/// DD-API only stores coordinates.
	/// </summary>
	/// <param name="geoJson"></param>
	/// <returns></returns>
	private static decimal[]? GeoJsonToDecimalArray(GeoJson? geoJson)
	{
		if (geoJson == null)
		{
			return null;
		}

		var point   = (Point)geoJson;
		return new[] { (decimal)point.Coordinates.Longitude, (decimal)point.Coordinates.Latitude };
	}
	
	/// <summary>
	/// Determines if the given position lies inside the bounding box. [0] and [2] represent longitude, [1] and [3] represent latitude. Longitude translates to X, latitude translated to Y.
	/// </summary>
	/// <param name="position">Position</param>
	/// <param name="boundingBox">Bounding box</param>
	/// <returns>true if position is inside the bounding box, false otherwise.</returns>
	private static bool InBoundingBox(decimal[]? position, decimal[] boundingBox)
	{
		if (position is not { Length: 2 } || boundingBox.Length != 4)
		{
			return false;
		}

		return position[0] >= boundingBox[0] && position[0] <= boundingBox[3] && position[1] >= boundingBox[1] && position[1] <= boundingBox[3];
	}


	/// <summary>
	///   Filter measurements on values.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="measurements">Measurements to be filtered.</param>
	/// <param name="connectorCapabilities"></param>
	private static void FilterValues(IEnumerable<Condition>? conditions, List<Measurement> measurements, List<ConnectorCapability> connectorCapabilities)
	{
		if (conditions == null)
		{
			return;
		}
			
		var filteredConditions = conditions.Where(a => a.FieldName == ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.Value)).ToList();
		if (!filteredConditions.Any())
		{
			return;
		}

		foreach (var condition in filteredConditions)
		{
			switch (condition.CompareMethod)
			{
				case (CompareMethod.Eq):
					measurements.RemoveAll(a => a.Value != condition.CompareDataAsNumber());
					break;
				case (CompareMethod.Ne):
					measurements.RemoveAll(a => a.Value == condition.CompareDataAsNumber());
					break;
				case (CompareMethod.Lt):
					measurements.RemoveAll(a => a.Value >= condition.CompareDataAsNumber());
					break;
				case (CompareMethod.Le):
					measurements.RemoveAll(a => a.Value > condition.CompareDataAsNumber());
					break;
				case (CompareMethod.Ge):
					measurements.RemoveAll(a => a.Value < condition.CompareDataAsNumber());
					break;
				case (CompareMethod.Gt):
					measurements.RemoveAll(a => a.Value <= condition.CompareDataAsNumber());
					break;
			}			
		}
	}

	/// <summary>
	///   Filter measurements on compartments.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="measurements">Measurements to be filtered.</param>
	/// <param name="connectorCapabilities"></param>
	private static void FilterCompartments(IEnumerable<Condition> conditions, List<Measurement> measurements, List<ConnectorCapability> connectorCapabilities)
	{
		var filteredConditions = conditions.Where(a => a.FieldName == ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.Compartment)).ToList(); // Eq already filtered
		if (!filteredConditions.Any())
		{
			return;
		}

		foreach (var condition in filteredConditions)
		{
			switch (condition.CompareMethod)
			{
				case (CompareMethod.Ne):
					measurements.RemoveAll(a => (a.Compartment ?? "") == condition.CompareDataAsString());
					break;
				case (CompareMethod.Like):
					measurements.RemoveAll(a => !(a.Compartment ?? "").Contains(condition.CompareDataAsString()));
					break;
				case (CompareMethod.Startswith):
					measurements.RemoveAll(a => !(a.Compartment ?? "").StartsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.Endswith):
					measurements.RemoveAll(a => !(a.Compartment ?? "").EndsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.In):
					measurements.RemoveAll(a => !condition.CompareDataAsArrayOfString().Contains(a.Compartment ?? ""));
					break;
				case (CompareMethod.Notin):
					measurements.RemoveAll(a => condition.CompareDataAsArrayOfString().Contains(a.Compartment ?? ""));
					break;
			}
		}
	}
		
	/// <summary>
	///   Filter dates.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="measurements">Measurements to be filtered.</param>
	/// <param name="connectorCapabilities"></param>
	private static void FilterDates(IEnumerable<Condition> conditions, List<Measurement> measurements, List<ConnectorCapability> connectorCapabilities)
	{
		var filteredConditions = conditions.Where(a => a.FieldName == ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.MeasurementDate)).ToList();
		if (!filteredConditions.Any())
		{
			return;
		}

		foreach (var condition in filteredConditions)
		{
			var minValue     = DateTimeOffset.MinValue;
			var maxValue     = DateTimeOffset.MaxValue;
			var compareValue = condition.CompareDataAsDateTimeOffset();
			switch (condition.CompareMethod)
			{
				case (CompareMethod.Ne):
					maxValue = compareValue.Date;
					minValue = compareValue.Date.AddDays(1).AddSeconds(-1);
					break;
				case (CompareMethod.Eq):
					minValue = compareValue.Date;
					maxValue = compareValue.Date.AddDays(1).AddSeconds(-1);
					break;
				case (CompareMethod.Ge):
					minValue = compareValue;
					break;
				case (CompareMethod.Gt):
					minValue = compareValue;
					break;
				case (CompareMethod.Le):
					maxValue = compareValue;
					break;
				case (CompareMethod.Lt):
					maxValue = compareValue;
					break;
			}
			
			measurements.RemoveAll(a => a.MeasurementDate < minValue || a.MeasurementDate > maxValue);			}
	}
		
	/// <summary>
	///   Filter parameters.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="measurements">Measurements to be filtered.</param>
	/// <param name="connectorCapabilities"></param>
	private static void FilterParameters(IEnumerable<Condition> conditions, List<Measurement> measurements, List<ConnectorCapability> connectorCapabilities)
	{
		var filteredConditions = conditions.Where(a => a.FieldName == ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.Parameter)).ToList();
		if (!filteredConditions.Any())
		{
			return;
		}

		foreach (var condition in filteredConditions)
		{
			switch (condition.CompareMethod)
			{
				case (CompareMethod.Eq):
					measurements.RemoveAll(a => a.Parameter != condition.CompareDataAsString());
					break;
				case (CompareMethod.Ne):
					measurements.RemoveAll(a => a.Parameter == condition.CompareDataAsString());
					break;
				case (CompareMethod.Startswith):
					measurements.RemoveAll(a => !(a.Parameter ?? string.Empty).StartsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.Endswith):
					measurements.RemoveAll(a => !(a.Parameter ?? string.Empty).EndsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.In):
					measurements.RemoveAll(a => !condition.CompareDataAsArrayOfString().Contains(a.Parameter ?? ""));
					break;
				case (CompareMethod.Notin):
					measurements.RemoveAll(a => condition.CompareDataAsArrayOfString().Contains(a.Parameter ?? ""));
					break;
			}
		}
	}

	/// <summary>
	///   Filter units.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="measurements">Measurements to be filtered.</param>
	/// <param name="connectorCapabilities"></param>
	private static void FilterUnits(IEnumerable<Condition> conditions, List<Measurement> measurements, List<ConnectorCapability> connectorCapabilities)
	{
		var filteredConditions = conditions.Where(a => a.FieldName == ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.Unit)).ToList();
		if (!filteredConditions.Any())
		{
			return;
		}

		foreach (var condition in filteredConditions)
		{
			switch (condition.CompareMethod)
			{
				case (CompareMethod.Eq):
					measurements.RemoveAll(a => a.Unit != condition.CompareDataAsString());
					break;
				case (CompareMethod.Ne):
					measurements.RemoveAll(a => a.Unit == condition.CompareDataAsString());
					break;
				case (CompareMethod.Startswith):
					measurements.RemoveAll(a => !(a.Unit ?? string.Empty).StartsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.Endswith):
					measurements.RemoveAll(a => !(a.Unit ?? string.Empty).EndsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.In):
					measurements.RemoveAll(a => !condition.CompareDataAsArrayOfString().Contains(a.Unit ?? ""));
					break;
				case (CompareMethod.Notin):
					measurements.RemoveAll(a => condition.CompareDataAsArrayOfString().Contains(a.Unit ?? ""));
					break;
			}
		}
	}

	/// <summary>
	///   Filter quantities.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="measurements">Measurements to be filtered.</param>
	/// <param name="connectorCapabilities"></param>
	private static void FilterQuantities(IEnumerable<Condition> conditions, List<Measurement> measurements, List<ConnectorCapability> connectorCapabilities)
	{
		var filteredConditions = conditions.Where(a => a.FieldName == ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.Quantity)).ToList();
		if (!filteredConditions.Any())
		{
			return;
		}

		foreach (var condition in filteredConditions)
		{
			switch (condition.CompareMethod)
			{
				case (CompareMethod.Eq):
					measurements.RemoveAll(a => a.Quantity != condition.CompareDataAsString());
					break;
				case (CompareMethod.Ne):
					measurements.RemoveAll(a => a.Quantity == condition.CompareDataAsString());
					break;
				case (CompareMethod.Startswith):
					measurements.RemoveAll(a => !(a.Quantity ?? string.Empty).StartsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.Endswith):
					measurements.RemoveAll(a => !(a.Quantity ?? string.Empty).EndsWith(condition.CompareDataAsString()));
					break;
				case (CompareMethod.In):
					measurements.RemoveAll(a => !condition.CompareDataAsArrayOfString().Contains(a.Quantity ?? ""));
					break;
				case (CompareMethod.Notin):
					measurements.RemoveAll(a => condition.CompareDataAsArrayOfString().Contains(a.Quantity ?? ""));
					break;
			}
		}
	}

	/// <summary>
	/// Create the request that is send to the service by adding page and page size..
	/// </summary>
	/// <param name="baseUrl">Base URL</param>
	/// <param name="request">Request</param>
	/// <param name="page">Page</param>
	/// <param name="pagesize">Page size</param>
	/// <returns></returns>
	private static string CreateRequest(string baseUrl, string request, int page, int pagesize)
	{
		var builder = new StringBuilder(baseUrl);
		var sep     = request.StartsWith("?") ? "&" : "?";
		if (!string.IsNullOrEmpty(request))
		{
			builder.Append(sep);
			builder.Append(request);
			sep = "&";
		}

		builder.Append($"{sep}page={page}");
		builder.Append($"&pageSize={pagesize}");
		return builder.ToString();
	}

	/// <summary>
	///   Send the paged request to DD-API and process the response.
	/// </summary>
	/// <param name="mappings">Source mappings</param>
	/// <param name="dataBody">DataBody as provided by the source</param>
	/// <param name="client">HTTP Client</param>
	/// <param name="measurementResponse">Response cache</param>
	/// <param name="request"></param>
	private static async Task ProcessRequestAsync(List<Map> mappings, DataBody dataBody, HttpClient client, MeasurementResponse measurementResponse, string request)
	{
		// Direct processing. Only for :eq: operators, since DD-API only knows those.
		if (dataBody.Conditions == null)
		{
			return;
		}

		if (dataBody.SourceDefinition.DdApiConfigurationSection == null)
		{
			return;
		}

		// Retrieve the data in pages.
		bool endReached;
		var  page = 1;
		do
		{
			try
			{
				var fullRequest = CreateRequest($"{dataBody.SourceDefinition.Url}/timeseries", request, page, dataBody.SourceDefinition.DdApiConfigurationSection.PageSize);
				var start             = DateTimeOffset.Now;
				var queryHttpResponse = await client.GetAsync(fullRequest).ConfigureAwait(false);
				if (!queryHttpResponse.IsSuccessStatusCode)
				{
					if (queryHttpResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
					{
						measurementResponse.Errors.Add("Authentication failed");
						measurementResponse.HttpStatus = (int)queryHttpResponse.StatusCode;
						return;
					}
				}

				measurementResponse.Statistics.ResponseTimeInMilliSeconds += (long)(DateTime.Now - start).TotalMilliseconds;
				page++;
				endReached = await HttpResponseDataToCacheResponseAsync(queryHttpResponse, measurementResponse, dataBody.Conditions, dataBody.SourceDefinition.Name, mappings, dataBody.ConnectorCapabilities).ConfigureAwait(false);
			}
			catch (InvalidOperationException e)
			{
				measurementResponse.Errors.Add($"Invalid operation exception: {e.Message}");
				return;
			}
			catch (HttpRequestException e)
			{
				measurementResponse.Errors.Add($"Request exception: {e.Message}");
			return;
			}
			catch (TaskCanceledException e)
			{
				measurementResponse.Errors.Add($"Task cancelled exception: {e.Message}");
				return;
			}
		} while (!endReached);
	}

	/// <summary>
	///   Produce a list of queries to be submitted to DD-API. This handles the DD-API V2operators for fields known by DD-API.
	/// </summary>
	/// <param name="mappings">Source mappings</param>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="knownFilters"></param>
	/// <param name="supportedParameters"></param>
	/// <param name="eqRequest"></param>
	/// <param name="connectorCapabilities"></param>
	/// <returns>List of query parts.</returns>
	private static List<string> ConstructQueriesForKnownInOperatorsKnownByDdApi(IReadOnlyCollection<Map> mappings, IReadOnlyCollection<Condition>? conditions, IReadOnlyDictionary<string, string> knownFilters, List<string> supportedParameters, string eqRequest, List<ConnectorCapability> connectorCapabilities)
	{
		if (conditions == null || conditions.All(a => a.CompareMethod != CompareMethod.In))
		{
			return new() { eqRequest };
		}

		var values = new List<string>();
		foreach (var inCondition in conditions.Where(a => a.CompareMethod == CompareMethod.In))
		{
			foreach (var part in inCondition.CompareDataAsArrayOfString())
			{
				var dataCategory = ConnectorCapabilityLookup.GetDataCategoryForFieldName(connectorCapabilities, inCondition.FieldName);
				var translated   = TranslateToSource(mappings, dataCategory, part);
				if (!supportedParameters.Contains(ResponseService.DataCategoryToPropertyName(translated.DataCategory)))
				{
					values.Add(eqRequest);
					continue;
				}
				var segment = $"{eqRequest}&{knownFilters[ResponseService.DataCategoryToPropertyName(translated.DataCategory)]}={translated.Name}";
				values.Add(segment);
			}
		}
			
		return values.Distinct().ToList();
	}

	/// <summary>
	///   Construct the date filter part of the request.
	/// </summary>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="connectorCapabilities"></param>
	/// <returns>Constructed query.</returns>
	private static string AddMeasurementDateToRequest(IReadOnlyCollection<Condition> conditions, List<ConnectorCapability> connectorCapabilities)
	{
		if (conditions.All(a => a.FieldName != ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.MeasurementDate)))
		{
			return string.Empty;
		}

		var field     = ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, DataCategory.MeasurementDate);
		var startPart = conditions.Single(a => a.FieldName == field && a.CompareMethod is CompareMethod.Ge or CompareMethod.Gt);
		var endPart   = conditions.Single(a => a.FieldName == field && a.CompareMethod is CompareMethod.Le or CompareMethod.Lt);
		var start     = startPart.CompareDataAsDateTimeOffset;
		var end       = endPart.CompareDataAsDateTimeOffset;
			
		return $"&startTime={start:yyyy-MM-ddTHH:mm:ssK}&endTime={end:yyyy-MM-ddTHH:mm:ssK}";
	}

	/// <summary>
	///   Compose the query request for DD-API based on Filters.
	/// </summary>
	/// <param name="mappings"></param>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="conditionsToProcess">List of parameters to process in this part.</param>
	/// <param name="supportedQueryParameters">Query parameters that the source knows.</param>
	/// <param name="connectorCapabilities"></param>
	/// <returns>The composed query.</returns>
	private static StringBuilder ComposeRequest(List<Map>? mappings, IReadOnlyCollection<Condition>? conditions, IEnumerable<string> conditionsToProcess, List<string>? supportedQueryParameters, List<ConnectorCapability> connectorCapabilities)
	{
		if (conditions == null || !conditions.Any() || supportedQueryParameters == null || !supportedQueryParameters.Any())
		{
			return new();
		}

		var sep     = "?";
		var request = new StringBuilder();
		foreach (var condition in conditions.Where(a => conditionsToProcess.Contains(a.FieldName) && a.CompareMethod == CompareMethod.Eq && a.DataType == DataType.StringType)) // DD-API only knows about strings and EQ.
		{
			var dataCategory = ConnectorCapabilityLookup.GetDataCategoryForFieldName(connectorCapabilities, condition.FieldName);
			var translated   = TranslateToSource(mappings, dataCategory, condition.CompareDataAsString());
			if (supportedQueryParameters == null || !supportedQueryParameters.Contains(KnownDdApiFiltersAndFilterNames[ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, translated.DataCategory)]))
			{
				continue;
			}

			request.Append($"{sep}{KnownDdApiFiltersAndFilterNames[ConnectorCapabilityLookup.GetParameterForCategory(connectorCapabilities, translated.DataCategory)]}={JsonConvert.DeserializeObject<string>(conditions.Single(a => a.FieldName == condition.FieldName).CompareDataAsString())}");
			sep = "&";
		}
		return request;
	}

	/// <summary>
	///   Process a DD-API response.
	/// </summary>
	/// <param name="queryHttpResponse">Response from DD-API.</param>
	/// <param name="measurementResponse">Container for the measurements, errors and statistics.</param>
	/// <param name="conditions">Parsed C-API query conditions.</param>
	/// <param name="sourceName">C-API source name.</param>
	/// <param name="mappings">Source mappings</param>
	/// <param name="connectorCapabilities"></param>
	/// <returns>True if more data the end of data is not reached, false otherwise.</returns>
	private static async Task<bool> HttpResponseDataToCacheResponseAsync(HttpResponseMessage queryHttpResponse, MeasurementResponse measurementResponse, IReadOnlyCollection<Condition>? conditions, string sourceName, List<Map> mappings, List<ConnectorCapability> connectorCapabilities)
	{
		try
		{
			var content = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
			if (!queryHttpResponse.IsSuccessStatusCode)
			{
				measurementResponse.Statistics.TotalByteCount += content.Length;
				var error = JsonConvert.DeserializeObject<ErrorResponse>(content);
				if (error?.Title != null)
				{
					measurementResponse.Errors.Add(error.Title);
				}

				measurementResponse.HttpStatus = error!.Status;
				return true;
			}
				
			var data = JsonConvert.DeserializeObject<TimeSeriesResponse>(content);			
			if (data?.Results == null)
			{
				return false; // No valid response. Error?
			}

			var moreData     = !string.IsNullOrEmpty(data.Paging?.Next);
			var measurements = data.Results!.Select(a => EventToMeasurements(mappings, a)).SelectMany(a => a).ToList().Select(a => PropertiesToMeasurement(a, sourceName)).ToList();

			if (conditions != null)
			{
				FilterCompartments(conditions, measurements, connectorCapabilities);
				FilterValues(conditions, measurements, connectorCapabilities);
				FilterLocationNames(conditions, measurements, connectorCapabilities);
				FilterQuantities(conditions, measurements, connectorCapabilities);
				FilterUnits(conditions, measurements, connectorCapabilities);
				FilterParameters(conditions, measurements, connectorCapabilities);
				FilterDates(conditions, measurements, connectorCapabilities);
			}

			measurementResponse.Measurements.AddRange(measurements);
			measurementResponse.Statistics.NumberOfRequests++;
			measurementResponse.Statistics.TotalByteCount += content.Length;
				
			return !moreData;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}

	}

	/// <summary>
	///   Flatten an event to a measurement.
	/// </summary>
	/// <param name="mappings">Source mappings</param>
	/// <param name="result"></param>
	/// <returns>Converted measurements.</returns>
	private static List<Dictionary<string, object?>> EventToMeasurements(IReadOnlyCollection<Map> mappings, Result result)
	{
		var measurements = new List<Dictionary<string, object?>>();
		if (result.Events == null)
		{
			return measurements;
		}

		var locationCode = result.Location?.Properties?.LocationId ?? string.Empty;
		var coordinates  = result.Location?.Geometry?.Coordinates ?? new List<double> { 0, 0}.ToArray();
		if (coordinates[0] < -180 || coordinates[0] > 180 || coordinates[1] < -180 || coordinates[1] > 180) // Outside degree space. Might be in meters.
		{
			coordinates = CoordinateTransformation.TransFormCoordinate(28991, 4258, coordinates); // Convert Rijksdriehoekcoördinaten to WGS84.
		}
			
		measurements.AddRange(result.Events.Select(a => EventToMeasurement(result, a, locationCode, coordinates, mappings)));
		return measurements;
	}

	/// <summary>
	/// Convert an event by a property dictionary. This is done to be able to translate properties.
	/// </summary>
	/// <param name="result">The result: the level above the event.</param>
	/// <param name="event">The actual measurement.</param>
	/// <param name="locationCode">Location code</param>
	/// <param name="coordinates">Coordinates of the location.</param>
	/// <param name="mappings">Source mappings</param>
	/// <returns>Constructed property dictionary</returns>
	private static Dictionary<string, object?> EventToMeasurement(Result result, Event @event, string locationCode, double[] coordinates, IReadOnlyCollection<Map> mappings)
	{
		var dict = new Dictionary<string, object?>();
		RemapProperty(dict, DataCategory.Compartment, result.ObservationType?.Compartment, mappings);
		RemapProperty(dict, DataCategory.Quantity, result.ObservationType?.Quantity, mappings);
		RemapProperty(dict, DataCategory.Parameter, result.ObservationType?.ParameterCode, mappings);
		RemapProperty(dict, DataCategory.Unit, result.ObservationType?.Unit, mappings);
		dict.Add("timeStamp", @event.TimeStamp);
		dict.Add("value", @event.Value);
		RemapProperty(dict, DataCategory.MeasurementObject, locationCode, mappings);
		dict.Add("coordinates", JsonConvert.SerializeObject(coordinates, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()}));
		AddMissingProperties(dict);
		return dict;
	}

	private static void AddIfMissing(Dictionary<string, object?> dict, string key)
	{
		if (!dict.ContainsKey(key))
		{
			dict.Add(key, null);
		}
	}
		
	private static void AddMissingProperties(Dictionary<string, object?> dict)
	{
		AddIfMissing(dict, "compartment");
		AddIfMissing(dict, "quantity");
		AddIfMissing(dict, "parameterCode");
		AddIfMissing(dict, "unit");
		AddIfMissing(dict, "value");
		AddIfMissing(dict, "timeStamp");
		AddIfMissing(dict, "locationId");
		AddIfMissing(dict, "coordinates");
	}

	private static void RemapProperty(IDictionary<string, object?> dict, DataCategory category, string? value, IEnumerable<Map> mappings)
	{
		var (dataCategory, name) = TranslateToAquo(mappings, category, value);
		var sourceDataCategory = DataCategoryToPropertyName(category);
		var targetDataCategory = DataCategoryToPropertyName(dataCategory);

		dict.Remove(sourceDataCategory);
		dict.Remove(targetDataCategory);
		dict.Add(targetDataCategory, name);
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="mapDataCategory"></param>
	/// <returns></returns>
	private static string DataCategoryToPropertyName(DataCategory mapDataCategory)
	{
		return mapDataCategory switch
		{
			DataCategory.Other => "",
			DataCategory.Quantity => "quantity",
			DataCategory.Unit => "unit",
			DataCategory.Value => "value",
			DataCategory.MeasurementObject => "measurementobject",
			DataCategory.Parameter => "parameter",
			DataCategory.MeasurementDate => "timeStamp",
			DataCategory.Compartment => "compartment",
			_ => ""
		};
	}
		

	/// <summary>
	/// Convert the property dictionary, describing the data of the measurement, to a measurement..
	/// </summary>
	/// <param name="properties">Properties</param>
	/// <param name="source">Source name</param>
	/// <returns>Constructed measurement.</returns>
	private static Measurement PropertiesToMeasurement(IReadOnlyDictionary<string, object?> properties, string source)
	{
		var ordinates = GetPropertyDecimalArray(properties, "coordinates")?.ToArray() ?? new decimal[2];
		return new()
		{
			Compartment       = GetPropertyString(properties, "compartment"),
			Quantity          = GetPropertyString(properties, "quantity"),
			Parameter         = GetPropertyString(properties, "parameter"),
			Unit              = GetPropertyString(properties, "unit"),
			MeasurementDate   = GetPropertyDateTimeOffset(properties, "timeStamp"),
			Value             = GetPropertyDecimal(properties, "value"),
			MeasurementObject = GetPropertyString(properties, "measurementobject"),
			Source            = source,
			Coordinate        = new Point(new Position((double)ordinates[0], (double)ordinates[1]))
		};
	}

	private static decimal? GetPropertyDecimal(IReadOnlyDictionary<string, object?> properties, string key)
	{
		return properties.ContainsKey(key) ? Convert.ToDecimal(properties[key]) : null;
	}
		
	private static string? GetPropertyString(IReadOnlyDictionary<string, object?> properties, string key)
	{
		return properties.ContainsKey(key) ? properties[key]?.ToString() : null;
	}

	private static DateTimeOffset? GetPropertyDateTimeOffset(IReadOnlyDictionary<string, object?> properties, string key)
	{
		return properties.ContainsKey(key) ? Convert.ToDateTime(properties[key]?.ToString()) : null;
	}
		
	private static decimal[]? GetPropertyDecimalArray(IReadOnlyDictionary<string, object?> properties, string key)
	{
		return properties.ContainsKey(key) ? JsonConvert.DeserializeObject<decimal[]>(properties[key]?.ToString() ?? "[]") : new decimal[2];
	}
		
	/// <summary>
	/// Retrieve the observation types. This is the source for obtaining compartments, parameters, quantities and units.
	/// </summary>
	/// <param name="dataBody">DataBody as provided by the source</param>
	/// <returns>Combination of HttpStatus code and a list of observation types,</returns>
	private static async Task<(HttpStatusCode HttpStatus, Dictionary<string, DataCategory> ObservationTypes)> GetObservationTypesAsync(DataBody dataBody)
	{
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		var entities = new Dictionary<string, DataCategory>();
		if (statusCode != HttpStatusCode.OK)
		{
			return (statusCode, entities);
		}
	
		var client = new HttpClient { BaseAddress = new(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);


		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);
			
		if (dataBody.SourceDefinition.DdApiConfigurationSection == null)
		{
			return (HttpStatusCode.InternalServerError, new());
		}

		var page       = 1;
		var endReached = false;
			
		do
		{
			try
			{
				var queryHttpResponse = await client.GetAsync($"{dataBody.SourceDefinition.Url}/observationTypes?page={page}&pageSize={dataBody.SourceDefinition.DdApiConfigurationSection.PageSize}").ConfigureAwait(false);
				if (!queryHttpResponse.IsSuccessStatusCode)
				{
					if (queryHttpResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
					{
						return (queryHttpResponse.StatusCode, entities);
					}
				}
				page++;
				var content  = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
				var response = JsonConvert.DeserializeObject<ObservationTypeResponse>(content);
				if (response?.Results == null)
				{
					continue;
				}

				response.Results.ForEach(a => DecomposeObservationType(dataBody.SourceDefinition.MapData, entities, a));
					
				endReached = string.IsNullOrEmpty(response.Paging?.Next) ;
			}
			catch (Exception)
			{
				return (HttpStatusCode.InternalServerError, entities);
			}
		} while (!endReached);

		return (HttpStatusCode.OK, entities);
	}

	private static void DecomposeObservationType(List<Map>? maps, Dictionary<string, DataCategory> entities, ObservationType entity)
	{
		if (maps == null)
		{
			return;
		}
		
		if (entity.Compartment != null)
		{
			var translation = TranslateToAquo(maps, DataCategory.Compartment, entity.Compartment);
			AddTranslation(entities, translation);
		}
		if (entity.Unit != null)
		{
			var translation = TranslateToAquo(maps, DataCategory.Unit, entity.Unit);
			AddTranslation(entities, translation);
		}
		if (entity.ParameterCode != null)
		{
			var translation = TranslateToAquo(maps, DataCategory.Parameter, entity.ParameterCode);
			AddTranslation(entities, translation);
		}
		if (entity.Quantity != null)
		{
			var translation = TranslateToAquo(maps, DataCategory.Quantity, entity.Quantity);
			AddTranslation(entities, translation);
		}
	}

	private static void AddTranslation(Dictionary<string, DataCategory> entities, (DataCategory DataCategory, string? Name) translation)
	{
		if (translation.Name != null)
		{
			if (!entities.ContainsKey(translation.Name))
			{
				entities.Add(translation.Name, translation.DataCategory);
			}
		}
	}
	
	/// <summary>
	/// Get a list of measurement objects.
	/// </summary>
	/// <param name="dataBody">DataBody as provided by the source</param>
	/// <returns>Combination of HttpStatus code and a list of observation types,</returns>
	public static async Task<(int HttpStatus, List<MeasurementObject> Entities)> GetMeasurementObjectsAsync(DataBody dataBody)
	{
		var entities = new List<MeasurementObject>();
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, new());
		}
			
		if (dataBody.SourceDefinition.DdApiConfigurationSection == null)
		{
			return (501, new());
		}

		var client = new HttpClient { BaseAddress = new(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);
		
		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);
		
		var page       = 1;
		var endReached = false;
			
		do
		{
			try
			{
				var queryHttpResponse = await client.GetAsync($"{dataBody.SourceDefinition.Url}/locations?page={page}&pageSize={dataBody.SourceDefinition.DdApiConfigurationSection.PageSize}").ConfigureAwait(false);
				if (!queryHttpResponse.IsSuccessStatusCode && queryHttpResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
				{
					return ((int)queryHttpResponse.StatusCode, entities);
				}
					
				page++;
				var content  = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
				var response = JsonConvert.DeserializeObject<LocationResponse>(content);
				if (response?.Results == null)
				{
					continue;
				}

				entities.AddRange(response.Results.Select(a => new MeasurementObject { Source = dataBody.SourceDefinition.Code, Code = a.Properties?.LocationId ?? string.Empty, Name = a.Properties?.LocationName ?? string.Empty, Geometry = a.Geometry}));
				endReached = response.Results.Count < dataBody.SourceDefinition.DdApiConfigurationSection.PageSize;
			}
			catch (Exception)
			{
				return ((int)HttpStatusCode.InternalServerError, entities);
			}
		} while (!endReached);

		return ((int)HttpStatusCode.OK, entities);
	}

	/// <summary>
	/// Translate the combination of DataCategory and field name from AQUO to the source.If not present in the mapping, the input parameters are returned.
	/// </summary>
	/// <param name="aquoMaps">AQUO mappings as defined in the source.</param>
	/// <param name="category">Data category</param>
	/// <param name="fieldName">Field name</param>
	/// <returns>Translated combination of DataCategory and field name</returns>
	private static (DataCategory DataCategory, string? Name) TranslateToAquo(IEnumerable<Map>? aquoMaps, DataCategory category, string? fieldName)
	{
		if (aquoMaps == null)
		{
			return (category, fieldName);
		}
			
		var mapping = aquoMaps.SingleOrDefault(a => a.SourceDataCategory == category && a.SourceName == fieldName);
		return mapping == null ? (category, fieldName) : (mapping.CApiDataCategory, mapping.CapiName);
	}
		
	/// <summary>
	/// Translate the combination of DataCategory and field name from the source to AQUO.If not present in the mapping, the input parameters are returned.
	/// </summary>
	/// <param name="aquoMaps">AQUO mappings as defined in the source.</param>
	/// <param name="category">Data category</param>
	/// <param name="fieldName">Field name</param>
	/// <returns>Translated combination of DataCategory and field name</returns>
	private static (DataCategory DataCategory, string? Name) TranslateToSource(IEnumerable<Map>? aquoMaps, DataCategory category, string? fieldName)
	{
		if (aquoMaps == null)
		{
			return (category, fieldName);
		}
			
		var mapping = aquoMaps.SingleOrDefault(a => a.CApiDataCategory == category && a.CapiName == fieldName);
		return mapping == null ? (category, fieldName) : (mapping.SourceDataCategory, mapping.SourceName);
	}
}