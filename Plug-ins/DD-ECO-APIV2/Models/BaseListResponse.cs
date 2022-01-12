using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DD_ECO_API.Models;

/// <summary>
/// MeasurementBase class for list responses. It is comprised of a block with links, a block with provider data, and a list of entities that are retrieved.
/// </summary>
/// <typeparam name="T">MeasurementBase list response type.</typeparam>
public sealed class BaseListResponse<T> where T : class, new()
{
	/// <summary>
	/// Paging block, providing information relevant to paging the results.
	/// </summary>
	[JsonPropertyName("paging")] public Links? Paging { set; get; }

	/// <summary>
	/// Information about the provider.
	/// </summary>
	/// <example>EcoSys</example>
	[JsonPropertyName("provider")] public ProviderBlock Provider { set; get; } = new ProviderBlock();

	/// <summary>
	/// Query results.
	/// </summary>
	[Required] [JsonPropertyName("result")] public T[]? Result { get; set; }
} // end class
// end Models namespace