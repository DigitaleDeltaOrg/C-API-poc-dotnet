using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines a DD-API observation type.
/// </summary>
public class ObservationType
{
	/// <summary>
	/// Url of the observation type.
	/// </summary>
	[JsonProperty("url")] public string? Url { set; get; }
	/// <summary>
	/// Node of the observation type.
	/// </summary>
	[JsonProperty("node")] public Node? Node { set; get; }
	/// <summary>
	/// Id of the observation type.
	/// </summary>
	[JsonProperty("id")] public string? Id { set; get; }
	/// <summary>
	/// Quantity of the observation type.
	/// </summary>
	[JsonProperty("quantity")] public string? Quantity { set; get; }
	/// <summary>
	/// Unit of the observation type.
	/// </summary>
	[JsonProperty("unit")] public string? Unit { set; get; }
	/// <summary>
	/// Compartment of the observation type.
	/// </summary>
	[JsonProperty("compartment")] public string? Compartment { set; get; }
	/// <summary>
	/// Parameter of the observation type.
	/// </summary>
	[JsonProperty("parameterCode")] public string? ParameterCode { set; get; }
}