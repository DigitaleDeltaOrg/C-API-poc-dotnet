using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines a timeseries.
/// </summary>
public class Result
{
	/// <summary>
	/// Id of the timeseries.
	/// </summary>
	[JsonProperty("id")] public string? Id { set; get; }
	/// <summary>
	/// Url of the timeseries.
	/// </summary>
	[JsonProperty("url")] public string? Url { set; get; }
	/// <summary>
	/// Node of the timeseries.
	/// </summary>
	[JsonProperty("node")] public Node? Node { set; get; }
	/// <summary>
	/// Source of the timeseries.
	/// </summary>
	[JsonProperty("source")] public Source? Source { set; get; }
	/// <summary>
	/// Location of the timeseries.
	/// </summary>
	[JsonProperty("location")] public Location? Location { set; get; }
	/// <summary>
	/// Observation type of the timeseries.
	/// </summary>
	[JsonProperty("observationType")] public ObservationType? ObservationType { set; get; }
	/// <summary>
	/// Analysis time of the timeseries.
	/// </summary>
	[JsonProperty("analysisTime")] public DateTimeOffset? AnalysisTime { set; get; }
	/// <summary>
	/// Start time of the timeseries.
	/// </summary>
	[JsonProperty("startTime")] public DateTimeOffset? StartTime { set; get; }
	/// <summary>
	/// End time of the timeseries.
	/// </summary>
	[JsonProperty("endTime")] public DateTimeOffset? EndTime { set; get; }
	/// <summary>
	/// Realization of the timeseries.
	/// </summary>
	[JsonProperty("realization")] public long? Realization { set; get; }
	/// <summary>
	/// Events (time stamp + measured value) of the timeseries.
	/// </summary>
	[JsonProperty("events")] public List<Event>? Events { set; get; }
}