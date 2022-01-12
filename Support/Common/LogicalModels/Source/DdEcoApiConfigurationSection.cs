using System.Collections.Generic;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Specific configuration section for the DD-ECO-API plugins.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class DdEcoApiConfigurationSection
{
	/// <summary>
	/// Field definitions, known to exist by the plug-in.
	/// </summary>
	public List<FieldDefinition>? FieldDefinitions { set; get; } = new();
	/// <summary>
	/// Page size of the requests.
	/// </summary>
	/// <example>100</example>
	public int PageSize { init;        get; } = 1000;
	/// <summary>
	/// Use the capabilities of the Source. Ignores FieldDefinitions if true.
	/// </summary>
	public bool UseCapabilities { set; get; }
}