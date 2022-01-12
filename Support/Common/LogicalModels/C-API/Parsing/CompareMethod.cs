namespace Shared.LogicalModels.C_API.Parsing;

/// <summary>
/// Defines a compare method, which defines what type of comparison is to be used.
/// </summary>
public enum CompareMethod
{
	/// <summary>
	/// 0: Unknown - error.
	/// </summary>
	Unknown = 0,/// <summary>
	/// 1: Eq (equals)
	/// </summary>
	Eq = 1,
	/// <summary>
	/// 2: Ne (not equal)
	/// </summary>
	Ne = 2,
	/// <summary>
	/// 3: Lt (less than)
	/// </summary>
	Lt = 3,
	/// <summary>
	/// 4: Le (less than or equal to)
	/// </summary>
	Le = 4,
	/// <summary>
	/// 5:Ge (greater than of equal to)
	/// </summary>
	Ge = 5,
	/// <summary>
	/// 6: Gt (greater than)
	/// </summary>
	Gt = 6,
	/// <summary>
	/// 7: In (in list)
	/// </summary>
	In = 7,
	/// <summary>
	/// 8: Notin (not in list)
	/// </summary>
	Notin = 8,
	/// <summary>
	/// 9: Like
	/// </summary>
	Like = 9,
	/// <summary>
	/// 10: Starts with
	/// </summary>
	Startswith = 10,
	/// <summary>
	/// 11: Ends with
	/// </summary>
	Endswith = 11,
	/// <summary>
	/// 12: Wkt (WellKnownText)
	/// </summary>
	Wkt = 12,
	/// <summary>
	/// 13: GeoJson
	/// </summary>
	GeoJson = 13,
	/// <summary>
	/// 14: Bbox (bounding box)
	/// </summary>
	Bbox = 14

}