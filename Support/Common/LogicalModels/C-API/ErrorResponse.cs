using System.Collections.Generic;
using Shared.LogicalModels.C_API.Parsing;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Error response.
/// </summary>
public class ErrorResponse
{
	/// <summary>
	/// Errors from the sources.
	/// </summary>
	public List<SourceError> SourceErrors { set; get; } = new();
	/// <summary>
	/// Errors from the request.
	/// </summary>
	public List<ParserError> RequestError { set; get; } = new();
}