using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.LogicalModels.C_API.Parsing;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Combination of DataCategory and its name.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class Map
{
	/// <summary>
	/// Data category of property (from).
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public DataCategory CApiDataCategory { set; get; }

	/// <summary>
	/// Name of property (from).
	/// </summary>
	public string CapiName { set; get; } = string.Empty;
		
	/// <summary>
	/// Data category of property (from).
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public DataCategory SourceDataCategory { set; get; }

	/// <summary>
	/// Name of property (to).
	/// </summary>
	public string SourceName { set; get; } = string.Empty;

	/// <summary>
	/// Constructor.
	/// </summary>
	public Map()
	{
			
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="fromDataCategory"></param>
	/// <param name="capiName"></param>
	/// <param name="sourceCategory"></param>
	/// <param name="sourceName"></param>
	public Map(DataCategory fromDataCategory, string capiName, DataCategory sourceCategory, string sourceName)
	{
		CApiDataCategory   = fromDataCategory;
		CapiName           = capiName;
		SourceDataCategory = sourceCategory;
		SourceName         = sourceName;
	}
}