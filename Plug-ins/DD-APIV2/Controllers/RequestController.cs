using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DD_API.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PluginShared.Interfaces;
using Shared.LogicalModels.C_API;
using Shared.LogicalModels.MeasurementResponse;

namespace DD_API.Controllers;

/// <summary>
/// API allowing retrieval of measurements, compartments, units, parameters, quantities and measurement objects.
/// </summary>
[Route("v1")] 
[ApiController]
public class RequestController : ControllerBase, IBaseRequestController
{
	/// <summary>
	/// Retrieves a list of measurements that comply with the selection criteria.
	/// </summary>
	/// <param name="dataBody">Composed data body containing information for connection, authentication and translation</param>
	/// <returns>Response cache with retrieved (and filtered) data and statistics.</returns>
	[HttpPost]
	[Route("measurements")] 
	[ProducesResponseType(typeof(MeasurementResponse), 200)]
	[ProducesResponseType(typeof(ErrorResponse), 404)]
	[ProducesResponseType(typeof(ErrorResponse), 403)]
	[ProducesResponseType(typeof(ErrorResponse), 500)]
	[ProducesResponseType(typeof(ErrorResponse), 501)]
	public async Task<ActionResult<string>> PostQueryResultAsync([FromBody][Required] DataBody dataBody)
	{
		var response = await ResponseService.ProcessRequestAsync(dataBody).ConfigureAwait(false);
		return response.HttpStatus switch
		{
			200 => Ok(JsonConvert.SerializeObject(response, new JsonSerializerSettings { NullValueHandling          = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new CamelCasePropertyNamesContractResolver() })),
			404 => BadRequest(JsonConvert.SerializeObject(response, new JsonSerializerSettings { ContractResolver   = new CamelCasePropertyNamesContractResolver() })),
			403 => Unauthorized(JsonConvert.SerializeObject(response, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() })),
			500 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings { ContractResolver      = new CamelCasePropertyNamesContractResolver() }), null, 500, "Error"),
			501 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings { ContractResolver      = new CamelCasePropertyNamesContractResolver() }), null, 501, "Error"),
			_ => Problem()
		};
	}
		
	/// <summary>
	/// Retrieves a list of parameters known to the source service.
	/// </summary>
	/// <param name="dataBody">Composed data body containing information for connection, authentication and translation</param>
	/// <returns>List of known entities.</returns>
	[HttpPost]
	[Route("parameters")] 
	[ProducesResponseType(typeof(List<SourceCodeName>), 200)]
	[ProducesResponseType(typeof(ErrorResponse), 404)]
	[ProducesResponseType(typeof(ErrorResponse), 403)]
	[ProducesResponseType(typeof(ErrorResponse), 500)]
	[ProducesResponseType(typeof(ErrorResponse), 501)]
	public async Task<ActionResult<string>> GetParametersAsync([FromBody][Required] DataBody dataBody)
	{
		var response = await ResponseService.GetParametersAsync(dataBody).ConfigureAwait(false);
		return response.HttpStatus switch
		{
			200 => Ok(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() })),
			404 => BadRequest(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver    = new CamelCasePropertyNamesContractResolver()})),
			403 => Unauthorized(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			500 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 500, "Error"),
			501 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 501, "Error"),
			_   => Problem()
		};
	}
		
	/// <summary>
	/// Retrieves a list of compartments known to the source service.
	/// </summary>
	/// <param name="dataBody">Composed data body containing information for connection, authentication and translation</param>
	/// <returns>List of known entities.</returns>
	[HttpPost]
	[Route("compartments")] 
	[ProducesResponseType(typeof(List<SourceCodeName>), 200)]
	[ProducesResponseType(typeof(ErrorResponse), 404)]
	[ProducesResponseType(typeof(ErrorResponse), 403)]
	[ProducesResponseType(typeof(ErrorResponse), 500)]
	[ProducesResponseType(typeof(ErrorResponse), 501)]
	public async Task<ActionResult<string>> GetCompartmentsAsync([FromBody][Required] DataBody dataBody)
	{
		var response = await ResponseService.GetCompartmentsAsync(dataBody).ConfigureAwait(false);
		return response.HttpStatus switch
		{
			200 => Ok(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings { NullValueHandling       = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() })),
			404 => BadRequest(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			403 => Unauthorized(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			500 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 500, "Error"),
			501 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 501, "Error"),
			_   => Problem()
		};
	}
		
	/// <summary>
	/// Retrieves a list of units known to the source service.
	/// </summary>
	/// <param name="dataBody">Composed data body containing information for connection, authentication and translation</param>
	/// <returns>List of known entities.</returns>
	[HttpPost]
	[Route("units")] 
	[ProducesResponseType(typeof(List<SourceCodeName>), 200)]
	[ProducesResponseType(typeof(ErrorResponse), 404)]
	[ProducesResponseType(typeof(ErrorResponse), 403)]
	[ProducesResponseType(typeof(ErrorResponse), 500)]
	[ProducesResponseType(typeof(ErrorResponse), 501)]
	public async Task<ActionResult<string>> GetUnitsAsync([FromBody][Required] DataBody dataBody)
	{
		var response = await ResponseService.GetUnitsAsync(dataBody).ConfigureAwait(false);
		return response.HttpStatus switch
		{
			200 => Ok(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings { NullValueHandling       = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() })),
			404 => BadRequest(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			403 => Unauthorized(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			500 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 500, "Error"),
			501 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 501, "Error"),
			_   => Problem()
		};
	}
		
	/// <summary>
	/// Retrieves a list of quantities known to the source service.
	/// </summary>
	/// <param name="dataBody">Composed data body containing information for connection, authentication and translation</param>
	/// <returns>List of known entities.</returns>
	[HttpPost]
	[Route("quantities")] 
	[ProducesResponseType(typeof(List<SourceCodeName>), 200)]
	[ProducesResponseType(typeof(ErrorResponse), 404)]
	[ProducesResponseType(typeof(ErrorResponse), 403)]
	[ProducesResponseType(typeof(ErrorResponse), 500)]
	[ProducesResponseType(typeof(ErrorResponse), 501)]
	public async Task<ActionResult<string>> GetQuantitiesAsync([FromBody][Required] DataBody dataBody)
	{
		var response = await ResponseService.GetQuantitiesAsync(dataBody).ConfigureAwait(false);
		return response.HttpStatus switch
		{
			200 => Ok(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings { NullValueHandling       = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() })),
			404 => BadRequest(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			403 => Unauthorized(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			500 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 500, "Error"),
			501 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 501, "Error"),
			_   => Problem()
		};
	}
		
	/// <summary>
	/// Retrieves a list of measurement objects (locations) known to the source service.
	/// </summary>
	/// <param name="dataBody">Composed data body containing information for connection, authentication and translation</param>
	/// <returns>List of known entities.</returns>
	[HttpPost]
	[Route("measurementobjects")] 
	[ProducesResponseType(typeof(List<MeasurementObject>), 200)]
	[ProducesResponseType(typeof(ErrorResponse), 404)]
	[ProducesResponseType(typeof(ErrorResponse), 403)]
	[ProducesResponseType(typeof(ErrorResponse), 500)]
	[ProducesResponseType(typeof(ErrorResponse), 501)]
	public async Task<ActionResult<string>> GetMeasurementObjectsAsync([FromBody][Required] DataBody dataBody)
	{
		var response = await ResponseService.GetMeasurementObjectsAsync(dataBody).ConfigureAwait(false);
		return response.HttpStatus switch
		{
			200 => Ok(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings { NullValueHandling       = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() })),
			404 => BadRequest(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			403 => Unauthorized(JsonConvert.SerializeObject(response.Entities, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()})),
			500 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 500, "Error"),
			501 => Problem(JsonConvert.SerializeObject(response, new JsonSerializerSettings {ContractResolver               = new CamelCasePropertyNamesContractResolver()}), null, 501, "Error"),
			_   => Problem()
		};
	}
}