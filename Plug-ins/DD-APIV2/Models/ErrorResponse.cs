using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines a DD-API error response.
/// </summary>
public class ErrorResponse
{
	/// <summary>
	/// Provider of the service.
	/// </summary>
	[JsonProperty("provider")] public Provider? Provider { set; get; }
	/// <summary>
	/// Type of error.
	/// </summary>
	[JsonProperty("type")] public string? Type { set; get; }
	/// <summary>
	/// Title of the error.
	/// </summary>
	[JsonProperty("title")] public string? Title { set; get; }
	/// <summary>
	/// Http status.
	/// </summary>
	[JsonProperty("status")] public int Status { set; get; }
	/// <summary>
	/// Error details.
	/// </summary>
	[JsonProperty("detail")] public string? Detail { set; get; }
	/// <summary>
	/// Internal reference to the instance of the data.
	/// </summary>
	[JsonProperty("instance")] public string? Instance { set; get; }
}