using System;
using Newtonsoft.Json;

namespace DD_API.Models;

/// <summary>
/// Defines an event.
/// </summary>
public class Event
{
	/// <summary>
	/// Measurement taken at...
	/// </summary>
	[JsonProperty("timestamp")] public DateTimeOffset TimeStamp { set; get;  }
	/// <summary>
	/// Measured value.
	/// </summary>
	[JsonProperty("value")] public decimal Value { set; get; }
}