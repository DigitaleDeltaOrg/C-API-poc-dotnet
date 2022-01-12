using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// The provider of the service.
/// </summary>
public class Provider
{
	/// <summary>
	/// Name of the provider.
	/// </summary>
	[JsonProperty("name")] public string? Name { set; get; }
	/// <summary>
	/// Url for support.
	/// </summary>
	[JsonProperty("supportUrl")] public string? SupportUrl { set; get; }
	/// <summary>
	/// API version.
	/// </summary>
	[JsonProperty("apiVersion")] public string? ApiVersion { set; get; }
	/// <summary>
	/// Type of response.
	/// </summary>
	[JsonProperty("responseType")] public string? ResponseType { set; get; }
	/// <summary>
	/// Timestamp of the response.
	/// </summary>
	[JsonProperty("responseTimestamp")] public string? ResponseTimeStamp { set; get; }
}