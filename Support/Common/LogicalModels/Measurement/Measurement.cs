using System;
using System.Collections.Generic;
using BAMCIS.GeoJSON;

namespace Shared.LogicalModels.Measurement;

/// <summary>
/// Describes a measurement.
/// </summary>
public class Measurement
{
	/// <summary>
	/// Source, delivering the measurement.
	/// </summary>
	public string? Source { set; get; }
	/// <summary>
	/// Aquo compartment.
	/// </summary>
	public string? Compartment { set; get; }
	/// <summary>
	/// Aquo parameter.
	/// </summary>
	public string? Parameter { set; get; }
	/// <summary>
	/// When the measurement was performed.
	/// </summary>
	public DateTimeOffset? MeasurementDate { set; get; }
	/// <summary>
	/// Unit
	/// </summary>
	public string? Unit { set; get; }
	/// <summary>
	/// Quantity
	/// </summary>
	public string? Quantity { set; get; }
	/// <summary>
	/// Measured value.
	/// </summary>
	public decimal? Value { set; get; }
	/// <summary>
	/// Measurement object (location)
	/// </summary>
	public string? MeasurementObject { set; get; }
	/// <summary>
	/// Coordinates where measurements where taken.
	/// </summary>
	public GeoJson? Coordinate { set; get; }

	/// <summary>
	/// Additional data associated with the measurement as a dictionary of objects by string (key)t.
	/// </summary>
	public Dictionary<string, object>? AdditionalData { set; get; }
}