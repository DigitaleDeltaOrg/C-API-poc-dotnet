using Newtonsoft.Json;
using Shared.LogicalModels.Measurement;
using Shared.LogicalModels.MeasurementResponse;
using Shared.LogicalModels.Plugin;
using Shared.LogicalModels.Source;

namespace CApiJsonStorage;

/// <summary>
/// Implements a very simple, file based storage provider for C-API by serializing the JSON.
/// ***Do not use it in production: the authentication data should not be stored as plain text.***
/// </summary>
public class CApiJsonStore : StorageShared.IStorageProvider
{
	private readonly string _folder;
	private const    string PluginFolder = "plugins";
	private const    string SourceDefinitionFolder = "sourcedefinitions";
	private const    string ResultFolder = "results";

	/// <summary>
	/// Create the directory structure, if it does not exist.
	/// </summary>
	/// <param name="folder"></param>
	public CApiJsonStore(string folder)
	{
		_folder = folder;
		Directory.CreateDirectory(folder);
		Directory.CreateDirectory(Path.Combine(folder, PluginFolder));
		Directory.CreateDirectory(Path.Combine(folder, SourceDefinitionFolder));
		Directory.CreateDirectory(Path.Combine(folder, ResultFolder));
	}
	
	/// <summary>
	/// Read the plug-in files from the file system and convert them to a list of Plugin.
	/// </summary>
	/// <returns></returns>
	public async Task<List<Plugin>> GetPluginsAsync()
	{
		var plugins = Directory.EnumerateFiles(Path.Combine(_folder, PluginFolder)).ToList().Select(a => JsonConvert.DeserializeObject<Plugin>(File.ReadAllText(a)) ?? new Plugin()).ToList();
		return await Task.FromResult(plugins.ToList());
	}
	
	public async Task<List<SourceDefinition>> GetSourcesDefinitionsAsync()
	{
		var plugins = Directory.EnumerateFiles(Path.Combine(_folder, SourceDefinitionFolder)).ToList().Select(a => JsonConvert.DeserializeObject<SourceDefinition>(File.ReadAllText(a)) ?? new SourceDefinition()).ToList();
		return await Task.FromResult(plugins.ToList());
	}
	
	public async Task<List<Measurement>> GetMeasurementsOfResponseCachesForIdAsync(Guid id)
	{
		if (!Directory.Exists(Path.Combine(_folder, ResultFolder, id.ToString())))
		{
			Directory.CreateDirectory(Path.Combine(_folder, id.ToString()));
		}

		var measurements = new List<Measurement>();
		foreach (var file in Directory.EnumerateFiles(Path.Combine(_folder, ResultFolder, id.ToString())).ToList())
		{
			var fileResponseContent = JsonConvert.DeserializeObject<MeasurementResponse>(await File.ReadAllTextAsync(file));
			if (fileResponseContent == null)
			{
				continue;
			}
			measurements.AddRange(fileResponseContent.Measurements);
		}
		return await Task.FromResult(measurements.ToList());
	}

	public async Task<bool> AddCachedResponseForIdAsync(Guid id, MeasurementResponse cachedMeasurementResponse)
	{
		if (!Directory.Exists(Path.Combine(_folder, ResultFolder, id.ToString())))
		{
			Directory.CreateDirectory(Path.Combine(_folder, ResultFolder, id.ToString()));
		}
		
		await File.WriteAllTextAsync(Path.Combine(_folder, ResultFolder, id.ToString(), cachedMeasurementResponse.Source), JsonConvert.SerializeObject(cachedMeasurementResponse));
		return true;
	}

	public async Task CleanupResponseCacheForIdAsync(Guid id)
	{
		await Task.Run(() =>
		{
			Directory.Delete(Path.Combine(_folder, ResultFolder, id.ToString()), true);
		});
	}

	public async Task<Plugin?> GetPluginByCodeAsync(string code)
	{
		var fileName = Path.Combine(_folder, PluginFolder, $"{code}.json");
		return !File.Exists(fileName) ? null : JsonConvert.DeserializeObject<Plugin>(await File.ReadAllTextAsync(fileName));
	}

	public async Task<SourceDefinition?> GetSourceByCodeAsync(string code)
	{
		var fileName = Path.Combine(_folder, SourceDefinitionFolder, $"{code}.json");
		return !File.Exists(fileName) ? null : JsonConvert.DeserializeObject<SourceDefinition>(await File.ReadAllTextAsync(fileName));
	}
}