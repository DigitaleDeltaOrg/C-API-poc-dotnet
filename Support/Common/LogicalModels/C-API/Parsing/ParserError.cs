using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.LogicalModels.C_API.Parsing;

/// <summary>
///   An error definition consists of an error and its context (statement)
/// </summary>
public sealed class ParserError
{
	/// <summary>
	/// Constructs a parser error.
	/// </summary>
	/// <param name="errorType">Error type</param>
	/// <param name="context">Error context</param>
	public ParserError(ErrorType errorType, string context)
	{
		ErrorType = errorType;
		Context   = context;
	}

	/// <summary>
	/// Error type.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public ErrorType ErrorType { get; }

	/// <summary>
	/// Error text.
	/// </summary>
	public string ErrorText => ErrorType.ToString();

	/// <summary>
	/// Error context.
	/// </summary>
	public string Context { get; }
}
