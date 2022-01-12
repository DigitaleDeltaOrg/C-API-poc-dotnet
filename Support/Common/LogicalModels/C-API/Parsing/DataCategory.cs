namespace Shared.LogicalModels.C_API.Parsing;

/// <summary>
/// Type of data.
/// </summary>
public enum DataCategory
{
	/// <summary>
	/// 0: Other
	/// </summary>
	Other = 0,
	/// <summary>
	/// 1: Represents a quantity.
	/// </summary>
	Quantity = 1,
	/// <summary>
	/// 2: Represents a unit.
	/// </summary>
	Unit = 2,
	/// <summary>
	/// 3: Represents a measured value.
	/// </summary>
	Value = 3,
	/// <summary>
	/// 4: Represents a measurement object (location).
	/// </summary>
	MeasurementObject = 4,
	/// <summary>
	/// 5: Represents a parameter.
	/// </summary>
	Parameter = 5,
	/// <summary>
	/// 6: Represents a measurement data.
	/// </summary>
	MeasurementDate = 6,
	/// <summary>
	/// 7: Represents a compartment.
	/// </summary>
	Compartment = 7
}