using System.Collections.Generic;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Specific configuration section for the DD-API plugins.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class DdApiConfigurationSection
{
	/// <summary>
	/// Supported query parameters by the source.
	/// </summary>
	public List<string>? SupportedQueryParameters { set; get; }
	/// <summary>
	/// Page size of the requests.
	/// </summary>
	/// <example>100</example>
	public int PageSize { init; get; } = 1000;

}