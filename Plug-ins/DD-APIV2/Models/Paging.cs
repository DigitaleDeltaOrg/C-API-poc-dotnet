using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines a DD-API paging block.
/// </summary>
public class Paging
{
	/// <summary>
	/// Total number of items satisfying the query.
	/// </summary>
	[JsonProperty("totalObjectCount")] public long TotalObjectCount { set; get; }
	/// <summary>
	/// Link to the previous page.
	/// </summary>
	[JsonProperty("prev")] public string? Prev { set; get; }
	/// <summary>
	/// Link to the next page.
	/// </summary>
	[JsonProperty("next")] public string? Next { set; get; }
	/// <summary>
	/// Link to this request.
	/// </summary>
	[JsonProperty("self")] public string? Self { set; get; }
	/// <summary>
	/// Minimum page size.
	/// </summary>
	[JsonProperty("minPageSize")] public int MinPageSize { set; get; }
	/// <summary>
	/// Maximum page size.
	/// </summary>
	[JsonProperty("maxPageSize")] public int MaxPageSize { set; get; }
}