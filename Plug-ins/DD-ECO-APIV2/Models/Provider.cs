using System.Text.Json.Serialization;

namespace DD_ECO_API.Models;

/// <summary>
/// Gives information regarding the provider of the service.
/// </summary>
public sealed class ProviderBlock
{
	/// <summary>
	/// Name of the provider.
	/// </summary>
	/// <example>EcoSys</example>
	[JsonPropertyName("name")] public string? Name { set; get; }

	/// <summary>
	/// Support website of the provider.
	/// </summary>
	/// <example>https://titans.ladesk.com/</example>
	[JsonPropertyName("supportUrl")] public string? SupportUrl { set; get; }

	/// <summary>
	/// Version of the API.
	/// </summary>
	/// <example>2.0</example>
	[JsonPropertyName("apiVersion")] public string? ApiVersion { set; get; }

	/// <summary>
	/// Definition of the response type.
	/// </summary>
	/// <example>MeasurementListResponse</example>
	[JsonPropertyName("responseType")] public string? ResponseType { set; get; }
}