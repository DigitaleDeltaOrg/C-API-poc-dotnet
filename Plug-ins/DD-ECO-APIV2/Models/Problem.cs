using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DD_ECO_API.Models;

/// <summary>
/// Describes the problem the service ran into.
/// </summary>
public sealed class Problem
{
	/// <summary>
	///  Additional information.
	/// </summary>
	[JsonPropertyName("typeInfo")] public string? TypeInfo { get; set; }

	/// <summary>
	///  Title of the error.
	/// </summary>
	/// <example>Query filter error.</example>
	[JsonPropertyName("title")] public string? Title { get; set; }

	/// <summary>
	///  Status code (HTTP).
	/// </summary>
	/// <example>403</example>
	[JsonPropertyName("status")] public int Status { get; set; }
	 
	/// <summary>
	///  Problem details.
	/// </summary>
	/// <example>Filter syntax</example>
	[JsonPropertyName("detail")] public string? Detail { get; set; }

	/// <summary>
	///  Provides information to track the problem at the side of the provider.
	/// </summary>
	[JsonPropertyName("instance")] public string? Instance { get; set; }

	/// <summary>
	/// Information concerning the provider of the OpenAPI service.
	/// </summary>
	/// <example>EcoSys</example>
	[JsonPropertyName("provider")] public ProviderBlock Provider { get; set; } = new ProviderBlock();

	/// <summary>
	/// Errors encountered.
	/// </summary>
	[JsonPropertyName("errors")] public List<FriendlyError>? Errors { get; set; }
}