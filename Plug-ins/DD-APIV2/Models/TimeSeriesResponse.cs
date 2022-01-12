using System.Collections.Generic;
using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Represents a timeseries: a list of measure,emt taken at a regular interval.
/// </summary>
public class TimeSeriesResponse
{
	/// <summary>
	/// Provider of the data.
	/// </summary>
	[JsonProperty("provider")] public Provider? Provider { set; get; }
	/// <summary>
	/// Paging information.
	/// </summary>
	[JsonProperty("paging")] public Paging? Paging { set; get; }
	/// <summary>
	/// Resulting collection of time series.
	/// </summary>
	[JsonProperty("results")] public List<Result>? Results { set; get; }
}