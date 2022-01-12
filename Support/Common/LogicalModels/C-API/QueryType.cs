using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Query type determines what comparisons are allowed.
/// </summary>
public enum QueryType
{
	/// <summary>
	///
	/// Exact string, meaning eq, ne, in and notin
	/// </summary>
	StringExact = 1,
	/// <summary>
	/// Wildcard, meaning StringExact plus startswith, endswith and like.
	/// </summary>
	StringWildCard = 2,
	/// <summary>
	/// eq, ne, lt, gt, le, ge
	/// </summary>
	Numeric = 3,
	/// <summary>
	/// eq, ne, lt, gt, le, ge
	/// </summary>
	Date = 4,
	/// <summary>
	/// StringExact plus bbox, wkt and geojson
	/// </summary>
	Geo = 5
}