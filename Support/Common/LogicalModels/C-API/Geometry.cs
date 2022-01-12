using Newtonsoft.Json;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// 
/// </summary>
public class Geometry
{
	/// <summary>
	/// Geometry type: Feature, Point, etc.
	/// </summary>
	[JsonProperty("type")] public string? Type { set; get; }
	/// <summary>
	/// List of ordinates
	/// </summary>
	[JsonProperty("coordinates")] public double[]? Coordinates { set; get; }
}