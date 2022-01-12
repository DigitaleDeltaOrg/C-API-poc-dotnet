namespace Shared.LogicalModels.C_API.Parsing;

/// <summary>
///   Supported data types.
/// </summary>
public enum DataType
{
	/// <summary>
	/// 0: Unsupported - Error
	/// </summary>
	Unsupported = 0,
	/// <summary>
	/// 1: String
	/// </summary>
	StringType = 1,
	/// <summary>
	/// 2: Number
	/// </summary>
	NumericType = 2,
	/// <summary>
	/// 3: Date
	/// </summary>
	DateType = 3,
	/// <summary>
	/// 4: string[]
	/// </summary>
	ArrayOfStringType = 4,
	/// <summary>
	/// 5: number[]
	/// </summary>
	ArrayOfNumericType = 5,
	/// <summary>
	/// 6: bbox (bounding box)
	/// </summary>
	BboxType = 6,
	/// <summary>
	/// 7: polygon
	/// </summary>
	PolygonType = 7,
	/// <summary>
	/// 8: wkt
	/// </summary>
	WktType = 8,
	/// <summary>
	/// 9: GeoJson
	/// </summary>
	GeoJsonType = 9
}