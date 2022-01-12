using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shared.LogicalModels.C_API;

namespace PluginShared.Interfaces;

public interface IBaseRequestController
{
		public Task<ActionResult<string>> PostQueryResultAsync(DataBody       dataBody);
		public Task<ActionResult<string>> GetParametersAsync(DataBody         dataBody);
		public Task<ActionResult<string>> GetUnitsAsync(DataBody              dataBody);
		public Task<ActionResult<string>> GetMeasurementObjectsAsync(DataBody dataBody);
		public Task<ActionResult<string>> GetCompartmentsAsync(DataBody       dataBody);
		public Task<ActionResult<string>> GetQuantitiesAsync(DataBody         dataBody);
}