using System.Collections.Generic;
using Shared.LogicalModels.Source;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Defines a measurement response.
/// </summary>
public class Response
{
	/// <summary>
	/// Retrieved measurements.
	/// </summary>
	public List<Measurement.Measurement>? Measurements { set; get; }
	/// <summary>
	/// Statistics.
	/// </summary>
	public Statistics? Statistics { set; get; }
	/// <summary>
	/// Errors.
	/// </summary>
	public List<string>? Errors { set; get; }
}