using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Describes a DD-API source.
/// </summary>
public class Source
{
	/// <summary>
	/// Process.
	/// </summary>
	[JsonProperty("process")] public string? Process { set; get; }
	/// <summary>
	/// Institution.
	/// </summary>
	[JsonProperty("institution")] public Institution? Institution { set; get; }
}