using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;
using Shared.LogicalModels.C_API.Parsing;

namespace DD_ECO_API.Models;

/// <summary>
/// 
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class Filter
{
	/// <summary>
	/// 
	/// </summary>
	public string     FieldName { set; get; } = string.Empty;
	/// <summary>
	/// 
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public CompareMethod Comparer  { set; get; }
	/// <summary>
	/// 
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public DataType Datatype { set;  get; }
	/// <summary>
	/// 
	/// </summary>
	public string Description { set; get; } = string.Empty;
}