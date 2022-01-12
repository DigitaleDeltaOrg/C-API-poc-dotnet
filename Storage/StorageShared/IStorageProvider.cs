using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.LogicalModels.Measurement;
using Shared.LogicalModels.MeasurementResponse;
using Shared.LogicalModels.Plugin;
using Shared.LogicalModels.Source;
using StorageShared.Models;

namespace StorageShared;

public interface IStorageProvider
{
	public Task<List<Plugin>>           GetPluginsAsync();
	public Task<List<SourceDefinition>> GetSourcesDefinitionsAsync();
	public Task<List<Measurement>>      GetMeasurementsOfResponseCachesForIdAsync(Guid id);
	public Task<bool>                   AddCachedResponseForIdAsync(Guid               id, MeasurementResponse cachedMeasurementResponse);
	public Task                         CleanupResponseCacheForIdAsync(Guid            id);
	public Task<Plugin?>                GetPluginByCodeAsync(string                    code);
	public Task<SourceDefinition?>      GetSourceByCodeAsync(string                    code);

}
