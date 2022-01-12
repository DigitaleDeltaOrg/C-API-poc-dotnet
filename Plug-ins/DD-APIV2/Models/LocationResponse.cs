using System.Collections.Generic;
using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines a response from a DD-API location request.
/// </summary>
public class LocationResponse
{
	/// <summary>
	/// Provider of the service.
	/// </summary>
	[JsonProperty("provider")] public Provider? Provider { set; get; }
	/// <summary>
	/// Paging information.
	/// </summary>
	[JsonProperty("paging")] public Paging? Paging { set; get; }
	/// <summary>
	/// Retrieved locations.
	/// </summary>
	[JsonProperty("results")] public List<Location>? Results { set; get; }
}