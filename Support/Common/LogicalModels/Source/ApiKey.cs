namespace Shared.LogicalModels.Source;

/// <summary>
/// Defines an Api Key.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ApiKey
{
	/// <summary>
	/// API key passed to the source.
	/// </summary>
	public string Key { init; get; }
	/// <summary>
	/// The name of the header used to pass the API key.
	/// </summary>
	public string Header { init; get; }

	/// <summary>
	/// Constructs API Key data.
	/// </summary>
	/// <param name="key">API key</param>
	/// <param name="header">API key header</param>
	public ApiKey(string key, string header)
	{
		Key       = key;
		Header = header;
	}
}