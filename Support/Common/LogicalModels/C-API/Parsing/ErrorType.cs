namespace Shared.LogicalModels.C_API.Parsing;

/// <summary>
///   Possible error types.
/// </summary>
public enum ErrorType
{
	/// <summary>
	/// 1: Unknown field.
	/// </summary>
	UnknownField = 1,
	/// <summary>
	/// 2: Unknown compare method.
	/// </summary>
	UnknownCompareMethod = 2,
	/// <summary>
	/// 3: Invalid value.
	/// </summary>
	InvalidValue = 3,
	/// <summary>
	/// 4: Invalid bounding box.
	/// </summary>
	InvalidBbox = 4,
	/// <summary>
	/// 5: Invalid syntax.
	/// </summary>
	InvalidSyntax = 5,
	/// <summary>
	/// 6: Array contains duplicates
	/// </summary>
	ArrayContainsDuplicates = 6,
	/// <summary>
	/// 7: Array is empty.
	/// </summary>
	ArrayIsEmpty = 7,
	/// <summary>
	/// 8: Invalid polygon.
	/// </summary>
	InvalidPolygon = 8,
	/// <summary>
	/// 9: Invalid WellKnownText.
	/// </summary>
	InvalidWkt = 9,
	/// <summary>
	/// 10: Invalid data type.
	/// </summary>
	InvalidDataType = 10,
	/// <summary>
	/// 11: Required.
	/// </summary>
	Required = 11,
	/// <summary>
	/// 12: Quote mismatch.
	/// </summary>
	QuoteMismatch = 12,
	/// <summary>
	/// 13: Bracket mismatch.
	/// </summary>
	BracketMismatch = 13,
	/// <summary>
	/// 14: Filter is required.
	/// </summary>
	FilterRequired = 14,
	/// <summary>
	/// 15: Array is not specified.
	/// </summary>
	ArrayNotSpecified = 15,
	/// <summary>
	/// 16: Not quoted.
	/// </summary>
	NotQuoted = 16
}