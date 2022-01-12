using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// 
/// </summary>
public class SourceError
{
	/// <summary>
	/// Source giving the error.
	/// </summary>
	public string? Source { set; get; }
	/// <summary>
	/// Source error type.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public SourceErrorType Error { set; get;}
}