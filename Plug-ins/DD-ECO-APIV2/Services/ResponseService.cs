using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BAMCIS.GeoJSON;
using DD_ECO_API.Models;
using Newtonsoft.Json;
using PluginShared;
using Shared.LogicalModels.C_API;
using Shared.LogicalModels.C_API.Parsing;
using Shared.LogicalModels.Measurement;
using Shared.LogicalModels.Source;
using Shared.LogicalModels;
using Shared.LogicalModels.MeasurementResponse;
using Geometry = Shared.LogicalModels.C_API.Geometry;

namespace DD_ECO_API.Services;

internal static class ResponseService
{
	
	/// <summary>
	/// Main worker for interpreting and processing the C-API request.
	/// </summary>
	/// <param name="dataBody">Data send by POST of the HTTP request. Contains the FilterParser and the Source that the plugin needs to address.</param>
	/// <returns>ResponseCache containing status, errors and results of the request.</returns>
	public static async Task<MeasurementResponse> ProcessRequestAsync(DataBody dataBody)
	{
		var responseCache        = new MeasurementResponse { RequestId = dataBody.ResponseId, Source = dataBody.SourceDefinition.Code };
		var start                = DateTime.Now;
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			responseCache.Errors.Add("Authentication failed");
			return responseCache;
		}
		
		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);
		
		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);
		if (dataBody.SourceDefinition.DdEcoApiConfigurationSection == null)
		{
			responseCache.HttpStatus = 501;
			responseCache.Errors.Add("DdEcoApiConfigurationSection is missing.");
			return responseCache;
		}
		
		if (dataBody.SourceDefinition.DdEcoApiConfigurationSection.FieldDefinitions == null && !dataBody.SourceDefinition.DdEcoApiConfigurationSection.UseCapabilities)
		{
			responseCache.HttpStatus = 501;
			responseCache.Errors.Add("DdEcoApiConfigurationSection.FieldDefinitions is missing but UseCapabilities is false.");
			return responseCache;
		}

		var query = await ConstructQueryAsync(dataBody.SourceDefinition, dataBody.Conditions, dataBody.ConnectorCapabilities, dataBody.SourceDefinition.MapData).ConfigureAwait(false);
		await ProcessRequestAsync(query, client, responseCache, dataBody.SourceDefinition, dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize, dataBody.Conditions).ConfigureAwait(false);
		responseCache.HttpStatus                            = 200;
		responseCache.Statistics.Source                     = dataBody.SourceDefinition.Code;
		responseCache.Statistics.ResponseTimeInMilliSeconds = (DateTime.Now - start).Milliseconds;
		return responseCache;
	}

	/// <summary>
	/// Send the paged request to DD-ECO-API-V2 and process the DD-ECO-API V2.
	/// </summary>
	/// <param name="request">Query string</param>
	/// <param name="client">HTTP Client</param>
	/// <param name="measurementResponse">Response cache</param>
	/// <param name="sourceDefinition">Source</param>
	/// <param name="pageSize">Page size</param>
	/// <param name="conditions"></param>
	private static async Task ProcessRequestAsync(string request, HttpClient client, MeasurementResponse measurementResponse, SourceDefinition sourceDefinition, int pageSize, List<Condition>? conditions)
	{
		client.Timeout = TimeSpan.FromMinutes(1);
		bool endReached;
		var  page = 1;
		do
		{
			try
			{
				var query             = $"{sourceDefinition.Url}/measurements?{request}&page={page}&pagesize={pageSize}";
				var queryHttpResponse = await client.GetAsync(query).ConfigureAwait(false);
				if (!queryHttpResponse.IsSuccessStatusCode)
				{
					measurementResponse.Errors.Add($"Response: {await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false)}");
					return;
				}
				page++;
				endReached = await HttpResponseDataToCacheResponseAsync(queryHttpResponse, measurementResponse, sourceDefinition, conditions).ConfigureAwait(false);
				measurementResponse.Statistics.NumberOfRequests++;
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
			catch (Exception e)
			{
				Console.WriteLine(e);
				return;
			}
		} while (!endReached);
	}

	/// <summary>
	/// Process a DD-API response.
	/// </summary>
	/// <param name="queryHttpResponse">Response from DD-API</param>
	/// <param name="measurementResponse">Container for the measurements, errors and statistics</param>
	/// <param name="sourceDefinition">C-API translations</param>
	/// <param name="conditions"></param>
	/// <returns>True if more data the end of data is not reached, false otherwise</returns>
	private static async Task<bool> HttpResponseDataToCacheResponseAsync(HttpResponseMessage queryHttpResponse, MeasurementResponse measurementResponse, SourceDefinition sourceDefinition, List<Condition>? conditions)
	{
		var endReached = false;
		var content = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
		if (!queryHttpResponse.IsSuccessStatusCode)
		{
			measurementResponse.Statistics.TotalByteCount += content.Length;
			var error = JsonConvert.DeserializeObject<Problem>(content);
			if (error is { Title: { } })
			{
				measurementResponse.Errors.Add(error.Title);
			}

			measurementResponse.HttpStatus = error?.Status ?? 500;
			return true;
		}

		var response     = JsonDocument.Parse(content);
		var measurements = response.RootElement.GetProperty("result").EnumerateArray();
		var pagingBlockResponse = response.RootElement.GetProperty("paging").GetRawText();
		HandleMeasurements(measurementResponse, measurements, sourceDefinition, conditions);
		if (string.IsNullOrEmpty(pagingBlockResponse))
		{
			return endReached;
		}

		var pagingBlock = JsonConvert.DeserializeObject<Links>(pagingBlockResponse);
		if (pagingBlock != null)
		{
			endReached = string.IsNullOrEmpty(pagingBlock.Next);
		}

		return endReached;
	}

	private static void HandleMeasurements(MeasurementResponse measurementResponse, JsonElement.ArrayEnumerator measurements, SourceDefinition sourceDefinition, List<Condition>? conditions)
	{
		if (conditions == null)
		{
			return;
		}
		
		foreach (var measurement in measurements)
		{
			var entity  = new Measurement();
			var objects = measurement.EnumerateObject();
			HandleMeasurementProperties(objects, entity, sourceDefinition);
			if (!ValidConditionsForFilter(objects, conditions))
			{
				continue;
			}
			entity.Source = sourceDefinition.Code;
			measurementResponse.Measurements.Add(entity);
		}
	}

	private static bool ValidConditionsForFilter(JsonElement.ObjectEnumerator objects, List<Condition> conditions)
	{
		var isValid = true;
		foreach (var @object in objects)
		{
			foreach (var condition in conditions.Where(a => a.FieldName == @object.Name))
			{
				switch (condition.CompareMethod)
				{
					case CompareMethod.Unknown:
						break;
					case CompareMethod.Eq:
						isValid = HandleEq(isValid, condition, @object);
						break;
					case CompareMethod.Ne:
						isValid = HandleNe(isValid, condition, @object);
						break;
					case CompareMethod.Lt:
						isValid = HandleLt(isValid, condition, @object);
						break;
					case CompareMethod.Le:
						isValid = HandleLe(isValid, condition, @object);
						break;
					case CompareMethod.Ge:
						isValid = HandleGe(isValid, condition, @object);
						break;
					case CompareMethod.Gt:
						isValid = HandleGt(isValid, condition, @object);
						break;
					case CompareMethod.In:
						isValid = HandleIn(isValid, condition, @object);
						break;
					case CompareMethod.Notin:
						isValid = HandleNotIn(isValid, condition, @object);
						break;
					case CompareMethod.Like:
						isValid = HandleLike(isValid, condition, @object);
						break;
					case CompareMethod.Startswith:
						isValid = HandleStartsWith(isValid, condition, @object);
						break;
					case CompareMethod.Endswith:
						isValid = HandleEndWith(isValid, condition, @object);
						break;
					case CompareMethod.Wkt:
						break;
					case CompareMethod.GeoJson:
						break;
					case CompareMethod.Bbox:
						break;
				}

				if (!isValid)
				{
					return false;
				}
			}
		}

		return isValid;
	}

	private static bool HandleEndWith(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.StringType => condition.CompareDataAsString().EndsWith(@object.Value.GetString() ?? string.Empty),
			_ => false
		};
	}

	private static bool HandleStartsWith(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.StringType => condition.CompareDataAsString().StartsWith(@object.Value.GetString() ?? string.Empty),
			_ => false
		};
	}

	private static bool HandleLike(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.StringType => condition.CompareDataAsString().Contains(@object.Value.GetString() ?? string.Empty),
			_ => false
		};
	}

	private static bool HandleNotIn(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.ArrayOfStringType => !condition.CompareDataAsArrayOfString().Contains(@object.Value.GetString() ?? string.Empty),
			DataType.ArrayOfNumericType => !condition.CompareDataAsArrayOfNumber().Contains(@object.Value.GetDecimal()),
			_ => false
		};
	}

	private static bool HandleIn(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		var result = condition.DataType switch
		{
			DataType.ArrayOfStringType => condition.CompareDataAsArrayOfString().Contains(@object.Value.GetString() ?? string.Empty),
			DataType.ArrayOfNumericType => condition.CompareDataAsArrayOfNumber().Contains(@object.Value.GetDecimal()),
			_ => false
		};

		return result;
	}

	private static bool HandleGt(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.DateType => condition.CompareDataAsDateTimeOffset().Date <= DateTimeOffset.Parse(@object.Value.ToString()).Date,
			DataType.NumericType => condition.CompareDataAsNumber() <= @object.Value.GetDecimal(),
			_ => false
		};
	}

	private static bool HandleGe(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.DateType => condition.CompareDataAsDateTimeOffset().Date < DateTimeOffset.Parse(@object.Value.ToString()).Date,
			DataType.NumericType => condition.CompareDataAsNumber() < @object.Value.GetDecimal(),
			_ => false
		};
	}

	private static bool HandleLe(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.DateType => condition.CompareDataAsDateTimeOffset().Date >= DateTimeOffset.Parse(@object.Value.ToString()).Date,
			DataType.NumericType => condition.CompareDataAsNumber() >= @object.Value.GetDecimal(),
			_ => false
		};
	}

	private static bool HandleLt(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.DateType => condition.CompareDataAsDateTimeOffset().Date > DateTimeOffset.Parse(@object.Value.ToString()).Date,
			DataType.NumericType => condition.CompareDataAsNumber() > @object.Value.GetDecimal(),
			_ => false
		};
	}

	private static bool HandleNe(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.StringType => condition.CompareDataAsString() != @object.Value.GetString(),
			DataType.DateType => condition.CompareDataAsDateTimeOffset().Date != DateTimeOffset.Parse(@object.Value.ToString()).Date,
			DataType.NumericType => condition.CompareDataAsNumber() != @object.Value.GetDecimal(),
			_ => false
		};
	}

	private static bool HandleEq(bool isValid, Condition condition, JsonProperty @object)
	{
		if (!isValid)
		{
			return false;
		}
		
		return condition.DataType switch
		{
			DataType.StringType => condition.CompareDataAsString() == @object.Value.GetString(),
			DataType.DateType => condition.CompareDataAsDateTimeOffset().Date == DateTimeOffset.Parse(@object.Value.ToString()).Date,
			DataType.NumericType => condition.CompareDataAsNumber() == @object.Value.GetDecimal(),
			_ => false
		};
	}

	private static void HandleMeasurementProperties(JsonElement.ObjectEnumerator objects, Measurement entity, SourceDefinition sourceDefinition)
	{
		var handledProperties = new List<string>();
		HandleUnits(entity, objects, sourceDefinition, handledProperties);
		HandleValue(entity, objects, sourceDefinition, handledProperties);
		HandleCompartment(entity, objects, sourceDefinition, handledProperties);
		HandleDate(entity, objects, sourceDefinition, handledProperties);
		HandleMeasurementObject(entity, objects, sourceDefinition, handledProperties);
		HandleCoordinate(entity, objects, sourceDefinition, handledProperties);
		HandleParameter(entity, objects, sourceDefinition, handledProperties);
		HandleQuantity(entity, objects, sourceDefinition, handledProperties);
		HandleAdditionalProperties(entity, objects, sourceDefinition, handledProperties);
	}
	
	private static void HandleAdditionalProperties(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}

		entity.AdditionalData ??= new();
		foreach (var @object in objects.Where(@object => !handledProperties.Contains(@object.Name) && !entity.AdditionalData.ContainsKey(@object.Name)))
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
	
	private static void HandleQuantity(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions.Where(a => a.DataCategory == DataCategory.Quantity).ToList() ?? new List<FieldDefinition>();
		foreach (var @object in objects)
		{
			var translated = TranslateToCApi(sourceDefinition.MapData, DataCategory.Quantity, @object.Name);
			if (knownFields.Any(a => a.FieldName == translated.Name))
			{
				entity.Quantity = @object.Value.GetString();
				handledProperties.Add(@object.Name);
			} 
		}
	}

	private static void HandleParameter(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.DdEcoApiConfigurationSection.FieldDefinitions.Where(a => a.DataCategory == DataCategory.Parameter).ToList();
		foreach (var @object in objects)
		{
			var translated = TranslateToCApi(sourceDefinition.MapData, DataCategory.Parameter, @object.Name);
			if (knownFields.Any(a => a.FieldName == translated.Name))
			{
				entity.Parameter = @object.Value.GetString();
				handledProperties.Add(@object.Name);
			}
		}
	}

	private static void HandleCoordinate(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		foreach (var @object in objects.Where(a => a.Name == "measurementgeography"))
		{
			entity.Coordinate = GeoJson.FromJson(@object.Value.ToString());
			handledProperties.Add(@object.Name);
		}
	}

	private static void HandleMeasurementObject(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.DdEcoApiConfigurationSection.FieldDefinitions.Where(a => a.DataCategory == DataCategory.MeasurementObject).ToList();
		foreach (var @object in objects)
		{
			var translated = TranslateToCApi(sourceDefinition.MapData, DataCategory.MeasurementObject, @object.Name);
			if (knownFields.Any(a => a.FieldName == translated.Name))
			{
				entity.MeasurementObject = @object.Value.GetString();
				handledProperties.Add(@object.Name);
			}
		}
	}

	private static void HandleDate(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.DdEcoApiConfigurationSection.FieldDefinitions.Where(a => a.DataCategory == DataCategory.MeasurementDate).ToList();
		foreach (var @object in from @object in objects let translated = TranslateToCApi(sourceDefinition.MapData, DataCategory.MeasurementDate, @object.Name) where knownFields.Any(a => a.FieldName == translated.Name) select @object)
		{
			entity.MeasurementDate = JsonConvert.DeserializeObject<DateTimeOffset>(@object.Value.GetRawText());
			handledProperties.Add(@object.Name);
		}
	}

	private static void HandleCompartment(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.DdEcoApiConfigurationSection.FieldDefinitions.Where(a => a.DataCategory == DataCategory.Compartment).ToList();
		foreach (var @object in from @object in objects let translated = TranslateToCApi(sourceDefinition.MapData, DataCategory.Compartment, @object.Name) where knownFields.Any(a => a.FieldName == translated.Name) select @object)
		{
			entity.Compartment = @object.Value.GetString();
			handledProperties.Add(@object.Name);
		}
	}

	private static void HandleValue(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.DdEcoApiConfigurationSection.FieldDefinitions.Where(a => a.DataCategory == DataCategory.Value).ToList();
		foreach (var @object in from @object in objects let translated = TranslateToCApi(sourceDefinition.MapData, DataCategory.Value, @object.Name) where knownFields.Any(a => a.FieldName == translated.Name) select @object)
		{
			entity.Value = @object.Value.GetDecimal();
			handledProperties.Add(@object.Name);
		}
	}

	private static void HandleUnits(Measurement entity, JsonElement.ObjectEnumerator objects, SourceDefinition sourceDefinition, List<string> handledProperties)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection?.FieldDefinitions == null)
		{
			return;
		}
		
		var knownFields = sourceDefinition.DdEcoApiConfigurationSection.FieldDefinitions.Where(a => a.DataCategory == DataCategory.Unit).ToList();
		foreach (var @object in from @object in objects let translated = TranslateToCApi(sourceDefinition.MapData, DataCategory.Unit, @object.Name) where knownFields.Any(a => a.FieldName == translated.Name) select @object)
		{
			entity.Unit = @object.Value.GetString();
			handledProperties.Add(@object.Name);
		}
	}
	
	private static async Task<string> ConstructQueryAsync(SourceDefinition sourceDefinition, List<Condition>? filter, List<ConnectorCapability> connectorCapabilities, List<Map>? maps)
	{
		if (sourceDefinition.DdEcoApiConfigurationSection == null)
		{
			return string.Empty;
		}

		if (filter == null)
		{
			return string.Empty;
		}
		
		var sourceCapabilities = sourceDefinition.DdEcoApiConfigurationSection.UseCapabilities ? await GetFieldDefinitionsAsync(sourceDefinition).ConfigureAwait(false) : null;
		// Determine known capabilities 

		var builder = new StringBuilder("filter=");
		foreach (var condition in filter.Where(condition => !sourceDefinition.DdEcoApiConfigurationSection.UseCapabilities || sourceCapabilities == null || sourceCapabilities.Any(a => a.FieldName == condition.FieldName && a.Comparer == condition.CompareMethod)))
		{
			var dataCategory = ConnectorCapabilityLookup.GetDataCategoryForFieldName(connectorCapabilities, condition.FieldName);
			var translation  = TranslateToSource(maps, dataCategory, condition.FieldName);
			//condition.FieldName = TranslateToAquo(source.MapToAquo, condition.FieldName, false);
			AppendCondition(builder, condition, translation.Name ?? condition.FieldName);
		}

		return builder.ToString();
	}

	private static void AppendCondition(StringBuilder builder, Condition condition, string name)
	{
		builder.Append(name);
		builder.Append(ComparerToString(condition.CompareMethod));
		builder.Append(ValueToString(condition));
	}

	private static string ValueToString(Condition condition)
	{
		switch (condition.DataType)
		{
			case DataType.StringType:
				return $"'{condition.CompareDataAsString()}';";
			case DataType.NumericType:
				return $"{condition.CompareDataAsNumber()};";
			case DataType.DateType:
				return $"'{condition.CompareDataAsDateTimeOffset():yyyy-MM-dd}';";
			case DataType.ArrayOfStringType:
				return $"[{string.Join(",", condition.CompareDataAsArrayOfString().Select(a => $"'{a}'"))}];";
			case DataType.ArrayOfNumericType:
				return $"[{string.Join(",", condition.CompareDataAsArrayOfNumber().Select(a => a))}]';";
			case DataType.BboxType:
				return $"[{string.Join(",", condition.CompareDataAsArrayOfNumber().Select(a => a))}]';";
			case DataType.PolygonType:
				return $"[{string.Join(",", condition.CompareDataAsArrayOfNumber().Select(a => a))}]';";
			case DataType.WktType:
				return $"'{condition.CompareDataAsString()}';";
			default:
				return string.Empty;
		}
	}

	private static string ComparerToString(CompareMethod compareMethod)
	{
		return compareMethod switch
		{
			CompareMethod.Bbox       => ":bbox:",
			CompareMethod.Endswith   => ":endswith:",
			CompareMethod.Eq         => ":eq:",
			CompareMethod.Ge         => ":ge:",
			CompareMethod.Gt         => ":gt:",
			CompareMethod.In         => ":in:",
			CompareMethod.Le         => ":le:",
			CompareMethod.Like       => ":like:",
			CompareMethod.Lt         => ":lt:",
			CompareMethod.Notin      => ":notin:",
			CompareMethod.Startswith => ":startswith:",
			CompareMethod.GeoJson    => ":geojson:",
			CompareMethod.Ne         => ":ne:",
			CompareMethod.Wkt        => ":wkt:",
			_                     => string.Empty
		};
	}

	public static async Task<(int HttpStatus, List<SourceCodeNameParameterData> Entities)> GetParametersAsync(DataBody dataBody)
	{
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, new List<SourceCodeNameParameterData>());
		}

		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);

		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);
		
		var  entities = new List<SourceCodeNameParameterData>();
		try
		{
			var queryHttpResponse = await client.GetAsync($"{dataBody.SourceDefinition.Url}/parameters/minimal").ConfigureAwait(false);
			await GetParameterResponseAsync(dataBody.SourceDefinition, queryHttpResponse, entities).ConfigureAwait(false);
			return ((int)HttpStatusCode.OK, entities);
		}
		catch (Exception)
		{
			return ((int)HttpStatusCode.InternalServerError, new());
		}
	}
		
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetQuantitiesAsync(DataBody dataBody)
	{
		if (dataBody.SourceDefinition.DdEcoApiConfigurationSection == null)
		{
			return (501, new());
		}
		
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, new List<SourceCodeName>());
		}

		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);
		
		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);

		bool endReached;
		var  page     = 1;
		var  entities = new List<SourceCodeName>();
		do
		{
			try
			{
				var queryHttpResponse = await client.GetAsync($"{dataBody.SourceDefinition.Url}/quantities?page={page}&pagesize={dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize}&nocount=true").ConfigureAwait(false);
				page++;
				endReached = await GetGenericCodeResponseAsync(dataBody.SourceDefinition, queryHttpResponse, entities, dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize).ConfigureAwait(false);
			}
			catch (Exception)
			{
				return ((int)HttpStatusCode.InternalServerError, new());
			}
		} while (!endReached);

		return ((int)HttpStatusCode.OK, entities);
	}
		
	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetCompartmentsAsync(DataBody dataBody)
	{
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, new List<SourceCodeName>());
		}

		if (dataBody.SourceDefinition.DdEcoApiConfigurationSection == null)
		{
			return (501, new());
		}

		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);
		
		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);
		
		bool endReached;
		var  page     = 1;
		var  entities = new List<SourceCodeName>();
		do
		{
			try
			{
				var queryHttpResponse = await client.GetAsync($"{dataBody.SourceDefinition.Url}/compartments?page={page}&pagesize={dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize}&nocount=true").ConfigureAwait(false);
				page++;
				endReached = await GetGenericCodeResponseAsync(dataBody.SourceDefinition, queryHttpResponse, entities, dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize).ConfigureAwait(false);
			}
			catch (Exception)
			{
				return ((int)HttpStatusCode.InternalServerError, new());
			}
		} while (!endReached);

		return ((int)HttpStatusCode.OK, entities);
	}

	public static async Task<(int HttpStatus, List<SourceCodeName> Entities)> GetUnitsAsync(DataBody dataBody)
	{
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, new List<SourceCodeName>());
		}

		if (dataBody.SourceDefinition.DdEcoApiConfigurationSection == null)
		{
			return (501, new());
		}

		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);
		
		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);

		bool endReached;
		var  page     = 1;
		var  entities = new List<SourceCodeName>();
		do
		{
			try
			{
				var queryHttpResponse = await client.GetAsync($"{dataBody.SourceDefinition.Url}/units?page={page}&pagesize={dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize}&nocount=true").ConfigureAwait(false);
				page++;
				endReached = await GetGenericCodeResponseAsync(dataBody.SourceDefinition, queryHttpResponse, entities, dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize).ConfigureAwait(false);
			}
			catch (Exception)
			{
				return ((int)HttpStatusCode.InternalServerError, new());
			}
		} while (!endReached);

		return ((int)HttpStatusCode.OK, entities);
	}
		
	private static async Task<bool> GetGenericCodeResponseAsync(SourceDefinition sourceDefinition, HttpResponseMessage queryHttpResponse, List<SourceCodeName> entities, int pageSize)
	{
		var content = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
		if (!queryHttpResponse.IsSuccessStatusCode)
		{
			return true;
		}

		var response = JsonDocument.Parse(content);
		var objects  = response.RootElement.GetProperty("result").EnumerateArray();
		entities.AddRange(objects.Select(@object => new SourceCodeName { Source = sourceDefinition.Code, Code = @object.GetProperty("code").GetString() ?? string.Empty, Name = @object.GetProperty("name").GetString() ?? string.Empty }));
		return objects.Count() < pageSize;
	}
	
	private static async Task GetParameterResponseAsync(SourceDefinition sourceDefinition, HttpResponseMessage queryHttpResponse, List<SourceCodeNameParameterData> entities)
	{
		var content = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
		if (!queryHttpResponse.IsSuccessStatusCode)
		{
			return;
		}

		var response = JsonDocument.Parse(content);
		var objects  = response.RootElement.GetProperty("result").EnumerateArray();
		entities.AddRange(objects.Select(@object => new SourceCodeNameParameterData
		{
			Source = sourceDefinition.Code, 
			Code = @object.GetProperty("code").GetString() ?? string.Empty, 
			Name = GetPropertyStringOrEmptyString(@object, "name") ?? string.Empty, 
			ParameterType = GetPropertyStringOrEmptyString(@object, "parametertype"), 
			TaxonType = GetPropertyStringOrEmptyString(@object, "taxontype"), 
			TaxonGroup = GetPropertyStringOrEmptyString(@object, "taxongroup"), 
			Authors = GetPropertyStringOrEmptyString(@object, "authors")
		}));
	}

	private static string? GetPropertyStringOrEmptyString(JsonElement @object, string propertyName)
	{
		return @object.TryGetProperty(propertyName, out @object) ? @object.GetString() : null;
	}

	public static async Task<(int HttpStatus, IEnumerable<MeasurementObject> Entities)> GetMeasurementObjectsAsync(DataBody dataBody)
	{
		var (statusCode, token) = await LogonService.AuthenticateAsync(dataBody.SourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return ((int)statusCode, Enumerable.Empty<MeasurementObject>());
		}

		if (dataBody.SourceDefinition.DdEcoApiConfigurationSection == null)
		{
			return (501, new List<MeasurementObject>());
		}

		var client = new HttpClient { BaseAddress = new Uri(dataBody.SourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);

		LogonService.HandlePostLogin(dataBody.SourceDefinition, client, token);

		bool endReached;
		var  page     = 1;
		var  entities = new List<MeasurementObject>();
		do
		{
			try
			{
				var queryHttpResponse = await client.GetAsync($"{dataBody.SourceDefinition.Url}/measurementobjects?page={page}&pagesize={dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize}").ConfigureAwait(false);
				page++;
				endReached = await GetMeasurementObjectResponseAsync(dataBody, queryHttpResponse, entities, dataBody.SourceDefinition.DdEcoApiConfigurationSection.PageSize).ConfigureAwait(false);
			}
			catch (Exception)
			{
				return ((int)HttpStatusCode.InternalServerError, Enumerable.Empty<MeasurementObject>());
			}
		} while (!endReached);

		return ((int)HttpStatusCode.OK, entities);
	}
		
	private static async Task<bool> GetMeasurementObjectResponseAsync(DataBody dataBody, HttpResponseMessage queryHttpResponse, ICollection<MeasurementObject> entities, int pageSize)
	{
		var content = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
		if (!queryHttpResponse.IsSuccessStatusCode)
		{
			return true;
		}

		var response = JsonDocument.Parse(content);
		var objects  = response.RootElement.GetProperty("result").EnumerateArray();
		foreach (var @object in objects)
		{
			var geo = new Geometry { Coordinates        = JsonConvert.DeserializeObject<double[]>(@object.GetProperty("geography").GetProperty("coordinates").GetRawText()), Type = @object.GetProperty("geography").GetProperty("type").GetString()};
			entities.Add(new MeasurementObject { Source = dataBody.SourceDefinition.Code, Code = @object.GetProperty("code").GetString() ?? string.Empty, Name = @object.GetProperty("name").GetString() ?? string.Empty, Geometry = geo });
		}
		return objects.Count() < pageSize;
	}
	
	/// <summary>
	/// Translate the combination of DataCategory and field name from AQUO to the source.If not present in the mapping, the input parameters are returned.
	/// </summary>
	/// <param name="maps">AQUO mappings as defined in the source.</param>
	/// <param name="category">Data category</param>
	/// <param name="fieldName">Field name</param>
	/// <returns>Translated combination of DataCategory and field name</returns>
	private static (DataCategory DataCategory, string? Name) TranslateToCApi(IEnumerable<Map>? maps, DataCategory category, string? fieldName)
	{
		if (maps == null)
		{
			return (category, fieldName);
		}
			
		var mapping = maps.SingleOrDefault(a => a.SourceDataCategory == category && a.SourceName == fieldName);
		return mapping == null ? (category, fieldName) : (mapping.CApiDataCategory, mapping.CapiName);
	}
		
	/// <summary>
	/// Translate the combination of DataCategory and field name from the source to AQUO.
	/// If not present in the mapping, the input parameters are returned.
	/// </summary>
	/// <param name="maps">AQUO mappings as defined in the source.</param>
	/// <param name="category">Data category</param>
	/// <param name="fieldName">Field name</param>
	/// <returns>Translated combination of DataCategory and field name</returns>
	private static (DataCategory DataCategory, string? Name) TranslateToSource(IEnumerable<Map>? maps, DataCategory category, string? fieldName)
	{
		if (maps == null)
		{
			return (category, fieldName);
		}
			
		var mapping = maps.SingleOrDefault(a => a.CApiDataCategory == category && a.CapiName == fieldName);
		return mapping == null ? (category, fieldName) : (mapping.SourceDataCategory, mapping.SourceName);
	}

	/// <summary>
	/// Get the filter capabilities of the source.
	/// </summary>
	/// <param name="sourceDefinition"></param>
	/// <returns>Translated combination of DataCategory and field name</returns>
	private static async Task<List<Filter>> GetFieldDefinitionsAsync(SourceDefinition sourceDefinition)
	{
		var (statusCode, token) = await LogonService.AuthenticateAsync(sourceDefinition).ConfigureAwait(false);
		if (statusCode != HttpStatusCode.OK)
		{
			return new List<Filter>();
		}
		
		var url    = $"{sourceDefinition.Url}/measurements/filters";
		var client = new HttpClient { BaseAddress = new Uri(sourceDefinition.Url) };
		client.Timeout = TimeSpan.FromMinutes(5);
		
		LogonService.HandlePostLogin(sourceDefinition, client, token);

		try
		{
			var queryHttpResponse = await client.GetAsync(url).ConfigureAwait(false);
			var content = await queryHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
			return JsonConvert.DeserializeObject<List<Filter>>(content) ?? new List<Filter>();
		}
		catch (Exception)
		{
			return new();
		}
	}
}
