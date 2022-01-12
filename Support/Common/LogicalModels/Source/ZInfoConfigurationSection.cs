using System.Collections.Generic;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Specific configuration section for the Z-Info plugins.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ZInfoConfigurationSection
{
	/// <summary>
	/// List of required query parameters in the Z-Info request. Default these have a * value.
	/// </summary>
	public List<string>? ZInfoRequiredQueryParameters { set; get; }

	/// <summary>
	/// Name of the parameter specifying the start date.
	/// </summary>
	public string StartDateParameter { set; get; } = string.Empty;
	/// <summary>
	/// Name of the parameter specifying the end date.
	/// </summary>
	public string EndDateParameter { set; get; } = string.Empty;

	/// <summary>
	/// Map of parameters that are requites for the request.
	/// </summary>
	public List<InputParameterMapEntry> InputParameterMap { set; get; } = new();
	/// <summary>
	/// Endpoint information for the compartments request.
	/// </summary>
	public EndPointFieldNameDescriptionName? CompartmentRequest { set; get; }
	/// <summary>
	/// Endpoint information for the parameters request.
	/// </summary>
	public EndPointFieldNameDescriptionName? ParameterRequest { set; get; }
	/// <summary>
	/// Endpoint information for the unit request.
	/// </summary>
	public EndPointFieldNameDescriptionName? UnitRequest { set; get; }
	/// <summary>
	/// Endpoint information for the quantity request.
	/// </summary>
	public EndPointFieldNameDescriptionName? QuantityRequest { set; get; }
	/// <summary>
	/// Endpoint information for the measurement object request.
	/// </summary>
	public EndPointFieldNameDescriptionName? MeasurementObjectRequest { set; get; }
	/// <summary>
	/// Endpoint information for the measurements request.
	/// </summary>
	public string? MeasurementRequest { set;                   get; }
	/// <summary>
	/// Z-INFO has categories for a location: the name of the location is insufficient. This formatter combines it into a single element.
	/// </summary>
	public string? MeasurementObjectNameConstructorFormat { set; get; }

}