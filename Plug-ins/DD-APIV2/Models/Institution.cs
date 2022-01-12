using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines an institution.
/// </summary>
public class Institution
{
	/// <summary>
	/// Name.
	/// </summary>
	[JsonProperty("name")] public string? Name { set; get; }
	/// <summary>
	/// Description.
	/// </summary>
	[JsonProperty("description")] public string? Description { set; get; }
}