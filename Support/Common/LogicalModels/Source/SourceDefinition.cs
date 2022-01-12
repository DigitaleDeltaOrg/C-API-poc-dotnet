using System.Collections.Generic;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Defines a Source, a configuration item that describes how to communicate with a plug-in.
/// </summary>
public class SourceDefinition
{
	/// <summary>
	/// Name of the source.
	/// </summary>
	public string Name { init; get; } = string.Empty;
	
	/// <summary>
	/// Code of the source.
	/// </summary>
	public string Code { init; get; } = string.Empty;
	
	/// <summary>
	/// Code of the plugin.
	/// </summary>
	public string Plugin { init; get; } = string.Empty;
	
	/// <summary>
	/// The URL where the DD-API implementation lives.
	/// </summary>
	public string Url { init; get; } = string.Empty;
	
	/// <summary>
	/// Defines how authentication is handled.
	/// </summary>
	public AuthenticationData? AuthenticationData { init; get; } = new(); 
	
	/// <summary>
	/// Statistics
	/// </summary>
	public Statistics? Statistics { init; get; } = new();
	
	/// <summary>
	/// Maps properties to corrected Aquo equivalents
	/// </summary>
	public List<Map>? MapData { set; get; }
		
	/// <summary>
	/// Supported query parameters by the source.
	/// </summary>
	public List<string>? SupportedQueryParameters { set; get; }
	
	/// <summary>
	/// Z-Info-specific configuration section.
	/// </summary>
	public ZInfoConfigurationSection? ZInfoConfigurationSection { set; get; }
	
	/// <summary>
	/// DD-ECO-API-specific configuration section.
	/// </summary>
	public DdEcoApiConfigurationSection? DdEcoApiConfigurationSection { set; get; }
	
	/// <summary>
	/// DD-API-specific configuration section.
	/// </summary>
	public DdApiConfigurationSection? DdApiConfigurationSection { set; get; }
}