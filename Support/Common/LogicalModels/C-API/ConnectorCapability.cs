using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.LogicalModels.C_API.Parsing;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Defines the capability of the connector.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ConnectorCapability
{
	/// <summary>
	/// 
	/// </summary>
	public string       FieldName    { set; get; } = string.Empty;
	/// <summary>
	/// 
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public DataCategory DataCategory { set; get; }
	/// <summary>
	/// 
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public QueryType    QueryType    { set; get; }
	
	/// <summary>
	/// 
	/// </summary>
	public bool Required { set; get; }
}

