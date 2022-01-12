using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines a DD-API ndoe.
/// </summary>
public class Node
{
	/// <summary>
	/// Id of the node.
	/// </summary>
	[JsonProperty("id")] public string? Id { set; get; } 
	/// <summary>
	/// Name of the node.
	/// </summary>
	[JsonProperty("name")] public string? Name { set; get; }
	/// <summary>
	/// Url of the node.
	/// </summary>
	[JsonProperty("url")] public string? BaseUrl { set; get; }
	/// <summary>
	/// Description of the node,
	/// </summary>
	[JsonProperty("description")] public string? Description { set; get; }
}