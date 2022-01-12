using System;
using System.Collections.Generic;
using Shared.LogicalModels.Source;

namespace Shared.LogicalModels.MeasurementResponse;

/// <summary>
/// Measurement response.
/// </summary>
public class MeasurementResponse
{
	/// <summary>
	/// Code of the source.
	/// </summary>
	public string Source { init; get; }
	/// <summary>
	/// Id of the request.
	/// </summary>
	public Guid RequestId { set; get; }
	/// <summary>
	/// List of measurements.
	/// </summary>
	public List<Measurement.Measurement> Measurements { init; get; } = new();
	/// <summary>
	/// Statistics.
	/// </summary>
	public Statistics Statistics { init; get; } = new();
	/// <summary>
	/// List of errors.
	/// </summary>
	public List<string> Errors { init; get; } = new();
	/// <summary>
	/// Http status code.
	/// </summary>
	public int HttpStatus { set; get; }
}