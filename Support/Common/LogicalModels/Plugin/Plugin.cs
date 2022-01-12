namespace Shared.LogicalModels.Plugin;

/// <summary>
/// Defines a plug-in.
/// </summary>
public class Plugin
{
	/// <summary>
	/// Code of the plug-in.
	/// </summary>
	public string Code { set; get; } = string.Empty;
	/// <summary>
	/// Name (or description) of the plug-in.
	/// </summary>
	public string Name { set; get; } = string.Empty;
	/// <summary>
	/// Url where the plug-in lives.
	/// </summary>
	public string Url { set; get; } = string.Empty;
}