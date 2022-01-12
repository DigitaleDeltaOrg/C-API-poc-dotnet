using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.LogicalModels.C_API.Parsing;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Maps input parameters (from C-API) to the Z-Info equivalents.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class InputParameterMapEntry
{
	/// <summary>
	/// Name of the C-API input parameter.
	/// </summary>
	public string CapiName { set; get; } = string.Empty;
	
	/// <summary>
	/// Data category of the input parameter
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public DataCategory DataCategory { set; get; }

	/// <summary>
	/// Data type of the input parameter.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public DataType DataType { set; get; }
	
	/// <summary>
	/// Z-Info name equivalents
	/// </summary>
	public List<string> ZInfoName { set; get; } = new();
}