using System.Text.Json.Serialization;

namespace DD_ECO_API.Models;

/// <summary>
/// Friendly version of the error.
/// </summary>
public sealed class FriendlyError
{
	/// <summary>
	/// Error type of the error.
	/// </summary>
	/// <example>UnknownValue</example>
	[JsonPropertyName("errorType")] public string? ErrorType { get; set; }

	/// <summary>
	/// Error context.
	/// </summary>
	/// <example>quantity/DEFX</example>
	[JsonPropertyName("context")] public string? Context { get; set; }
}