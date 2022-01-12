using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// 
/// </summary>
public class Properties
{
	/// <summary>
	/// The node of the data.
	/// </summary>
	[JsonProperty("node")] public Node? Node { set; get; }
	/// <summary>
	/// Id of the location.
	/// </summary>
	[JsonProperty("locationId")] public string? LocationId { set; get; }
	/// <summary>
	/// Name of the location.
	/// </summary>
	[JsonProperty("locationName")] public string? LocationName { set; get; }
	/// <summary>
	/// Reference level of the location/
	/// </summary>
	[JsonProperty("referenceLevel")] public string? ReferenceLevel { set; get; }
	/// <summary>
	/// Coordinate Reference System of the location.
	/// </summary>
	[JsonProperty("crs")] public Crs? Crs { set; get; }
}