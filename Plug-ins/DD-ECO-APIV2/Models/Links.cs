using System.Text.Json.Serialization;

namespace DD_ECO_API.Models;

/// <summary>
/// Provides information for paging.
/// </summary>
public sealed class Links
{
	/// <summary>
	/// Hyperlink of the current page.
	/// </summary>
	/// <example>/v1/measurements?page=2&amp;pagesize=100&amp;filter=quantity:eq:'AANTAL'</example>
	[JsonPropertyName("self")]
	public string? Self { set; get; }

	/// <summary>
	/// Hyperlink of the previous page, if present.
	/// </summary>
	/// <example></example>
	/// <example>/v1/measurements?page=1&amp;pagesize=100&amp;filter=quantity:eq:'AANTAL'</example>
	[JsonPropertyName("prev")]
	public string? Prev { set; get; }

	/// <summary>
	/// Hyperlink of the next page, if present.
	/// </summary>
	/// <example>/v1/measurements?page=3&amp;pagesize=100&amp;filter=quantity:eq:'AANTAL'</example>
	[JsonPropertyName("next")]
	public string? Next { set; get; }

	/// <summary>
	/// Maximum page size.
	/// </summary>
	/// <example>10000</example>
	[JsonPropertyName("maxPageSize")] public int? MaxPageSize { get; set; }

	/// <summary>
	/// Default page size.
	/// </summary>
	/// <example>1000</example>
	[JsonPropertyName("defaultPageSize")]
	public int DefaultPageSize { get; set; }

	/// <summary>
	/// Minimum page size.
	/// </summary>
	/// <example>1</example>
	[JsonPropertyName("minPageSize")] public int MinPageSize { get; set; } = 1;

	/// <summary>
	/// Count of all entities that satisfy the filter requirements. Will be left out when nocount=true is specified.
	/// </summary>
	/// <example>53452</example>
	[JsonPropertyName("totalObjectCount")]
	public int? TotalObjectCount { get; set; }
}