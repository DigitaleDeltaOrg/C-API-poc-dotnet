using Newtonsoft.Json;
using Shared.LogicalModels.C_API;

namespace DD_API.Models;

/// <summary>
/// Defines a measurement object/location.
/// </summary>
public class Location
{
	/// <summary>
	/// Type, i.e. Point
	/// </summary>
	[JsonProperty("type")] public string? Type { set; get; }
	/// <summary>
	/// Geometry of the location.
	/// </summary>
	[JsonProperty("geometry")] public Geometry? Geometry { set; get; }
	/// <summary>
	/// Additional properties of the location.
	/// </summary>
	[JsonProperty("properties")] public Properties? Properties { set; get; }
}