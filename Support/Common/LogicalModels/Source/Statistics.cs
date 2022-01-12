using System.Collections.Generic;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Describes statistics of a source.
/// </summary>
public class Statistics
{
	/// <summary>
	/// Code of the source.
	/// </summary>
	public string Source { set; get; } = string.Empty;

	/// <summary>
	/// Total response time in seconds.
	/// </summary>
	public long ResponseTimeInMilliSeconds { set; get; }

	/// <summary>
	/// Total number of bytes retrieved.
	/// </summary>
	public long TotalByteCount { set; get; }
	/// <summary>
	/// Total number of requests.
	/// </summary>
	public long NumberOfRequests { set; get; }
}