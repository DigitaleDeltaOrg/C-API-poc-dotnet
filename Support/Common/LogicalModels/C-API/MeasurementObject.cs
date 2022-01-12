

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Measurement object.
/// </summary>
public class MeasurementObject : SourceCodeName
{
	/// <summary>
	/// Geometry of the measurement.
	/// </summary>
	public Geometry? Geometry { set; get; }
}