using System.Collections.Generic;
using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defined a DD-API observation type result.
/// </summary>
public class ObservationTypeResponse
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
	/// Retrieved observation types.
	/// </summary>
	[JsonProperty("results")] public List<ObservationType>? Results { set; get; }
}