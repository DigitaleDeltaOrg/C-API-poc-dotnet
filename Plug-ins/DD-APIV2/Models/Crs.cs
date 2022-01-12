using System.Collections.Generic;
using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Describes a Coordinate Reference System.
/// </summary>
public class Crs
{
	/// <summary>
	/// Type.
	/// </summary>
	[JsonProperty("type")] public string? Type { set; get; }
	/// <summary>
	/// Dictionary of properties.
	/// </summary>
	[JsonProperty("properties")] public Dictionary<string, string>? Properties { set; get; }
}