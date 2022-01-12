using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Connector.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Shared.LogicalModels.C_API;
using StorageShared;

namespace Connector.Controllers;

[ApiController] 
[Route("")] 
public class RequestController : ControllerBase
{
	private readonly IStorageProvider          _storageProvider;
	private readonly List<ConnectorCapability> _connectionCapabilities;

	public RequestController(IStorageProvider storageProvider, IConfiguration configuration)
	{
		_storageProvider        = storageProvider;
		_connectionCapabilities = configuration.GetSection("ConnectorCapabilities").Get<List<ConnectorCapability>>();
	}

	[Route("measurements")]
	[HttpGet] 
	[ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)] 
	[ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)] 
	public async Task<ActionResult<string?>> QueryAsync([FromQuery] string sources, [FromQuery] string request)
	{
		var id          = Guid.NewGuid();
		var service     = new RequestService(_storageProvider);
		var sourceCodes = sources.Split(",").ToList();
		var (contentErrorResponse, content) = await service.ProcessAsync(id, sourceCodes, request, _connectionCapabilities).ConfigureAwait(false);
		if (contentErrorResponse != null)
		{
			return BadRequest(contentErrorResponse);
		}
		return Ok(JsonConvert.SerializeObject(content));
	}
	
	[Route("compartments")]
	[HttpGet] 
	[ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)] 
	[ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)] 
	public async Task<ActionResult<string?>> GetCompartmentsAsync([FromQuery] string sources)
	{
		var service       = new RequestService(_storageProvider);
		var errorResponse = new ErrorResponse { SourceErrors = await service.ValidateSourcesAsync(sources).ConfigureAwait(false) };
			
		if (errorResponse.RequestError.Any() || errorResponse.SourceErrors.Any())
		{
			return BadRequest(errorResponse);
		}
			
		var sourceCodes = sources.Split(",").ToList();
		var (contentErrorResponse, content) = await service.GetItemsAsync(sourceCodes, "compartments").ConfigureAwait(false);
		if (contentErrorResponse != null)
		{
			return BadRequest(contentErrorResponse);
		}

		return Ok(JsonConvert.SerializeObject(content));
	}

	[Route("units")]
	[HttpGet] 
	[ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)] 
	[ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)] 
	public async Task<ActionResult<string?>> GetUnitsAsync([FromQuery] string sources)
	{
		var service       = new RequestService(_storageProvider);
		var errorResponse = new ErrorResponse { SourceErrors = await service.ValidateSourcesAsync(sources).ConfigureAwait(false) };
			
		if (errorResponse.RequestError.Any() || errorResponse.SourceErrors.Any())
		{
			return BadRequest(errorResponse);
		}
			
		var sourceCodes = sources.Split(",").ToList();
		var (contentErrorResponse, content) = await service.GetItemsAsync(sourceCodes, "units").ConfigureAwait(false);
		if (contentErrorResponse != null)
		{
			return BadRequest(contentErrorResponse);
		}

		return Ok(JsonConvert.SerializeObject(content));
	}

	[Route("parameters")]
	[HttpGet] 
	[ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)] 
	[ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)] 
	public async Task<ActionResult<string?>> GetParametersAsync([FromQuery] string sources)
	{
		var service       = new RequestService(_storageProvider);
		var errorResponse = new ErrorResponse { SourceErrors = await service.ValidateSourcesAsync(sources).ConfigureAwait(false) };
			
		if (errorResponse.RequestError.Any() || errorResponse.SourceErrors.Any())
		{
			return BadRequest(errorResponse);
		}
			
		var sourceCodes = sources.Split(",").ToList();
		var (contentErrorResponse, content) = await service.GetItemsAsync(sourceCodes, "parameters").ConfigureAwait(false);
		if (contentErrorResponse != null)
		{
			return BadRequest(contentErrorResponse);
		}

		return Ok(JsonConvert.SerializeObject(content));
	}

	[Route("quantities")]
	[HttpGet] 
	[ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)] 
	[ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)] 
	public async Task<ActionResult<string?>> GetQuantitiesAsync([FromQuery] string sources)
	{
		var service       = new RequestService(_storageProvider);
		var errorResponse = new ErrorResponse { SourceErrors = await service.ValidateSourcesAsync(sources).ConfigureAwait(false) };
			
		if (errorResponse.RequestError.Any() || errorResponse.SourceErrors.Any())
		{
			return BadRequest(errorResponse);
		}
			
		var sourceCodes = sources.Split(",").ToList();
		var (contentErrorResponse, content) = await service.GetItemsAsync(sourceCodes, "quantities").ConfigureAwait(false);
		if (contentErrorResponse != null)
		{
			return BadRequest(contentErrorResponse);
		}

		return Ok(JsonConvert.SerializeObject(content));
	}

	[Route("sources")]
	[HttpGet] 
	public async Task<ActionResult> GetSourcesAsync()
	{
		return Ok((await _storageProvider.GetSourcesDefinitionsAsync().ConfigureAwait(false)).Select(a => new { a.Code, a.Name }));
	}
	
	[Route("measurementobjects")]
	[HttpGet] 
	[ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)] 
	[ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)] 
	public async Task<ActionResult<string?>> GetMeasurementObjectsAsync([FromQuery] string sources)
	{
		var service       = new RequestService(_storageProvider);
		var errorResponse = new ErrorResponse { SourceErrors = await service.ValidateSourcesAsync(sources).ConfigureAwait(false) };
			
		if (errorResponse.RequestError.Any() || errorResponse.SourceErrors.Any())
		{
			return BadRequest(errorResponse);
		}
			
		var sourceCodes = sources.Split(",").ToList();
		var (contentErrorResponse, content) = await service.GetMeasurementObjectsAsync(sourceCodes).ConfigureAwait(false);
		if (contentErrorResponse != null)
		{
			return BadRequest(contentErrorResponse);
		}

		return Ok(JsonConvert.SerializeObject(content));
	}

	[Route("capabilities")]
	[HttpGet] 
	public ActionResult GetCapabilitiesAsync()
	{
		return Ok(_connectionCapabilities);
	}
}