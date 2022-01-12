using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Shared.LogicalModels.Source;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// MeasurementBase class for list responses. It is comprised of a block with links, a block with provider data, and a list of entities that are retrieved.
/// </summary>
public sealed class BaseListResponse
{
	/// <summary>
	/// Statistics.
	/// </summary>
	[JsonPropertyName("statistics")]
	public List<Statistics> Statistics { set; get; }
	/// <summary>
	/// Query results.
	/// </summary>
	[Required]
	[JsonPropertyName("result")]
	public List<Measurement.Measurement>? Result { get; set; }

	/// <summary>
	/// Ctor.
	/// </summary>
	public BaseListResponse()
	{
		Result     = new List<Measurement.Measurement>();
		Statistics = new List<Statistics>();
	}
} // end class