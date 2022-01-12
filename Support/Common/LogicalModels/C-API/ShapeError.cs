using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Defines a shape error.
/// </summary>
public class ShapeError
{
	/// <summary>
	/// Shape.
	/// </summary>
	public string? Shape { set; get; }
	/// <summary>
	/// Error type.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public SourceErrorType Error { set; get;}
}