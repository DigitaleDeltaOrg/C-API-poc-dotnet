using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.LogicalModels.C_API.Parsing;

/// <summary>
///   Defines a condition: a part of the filter that includes field name, operator and field value.
/// </summary>
public class Condition
{
	/// <summary>
	///   Constructor
	/// </summary>
	public Condition()
	{
		CompareData = null;
		Errors      = new();
	}

	/// <summary>
	/// Construct a condition.
	/// </summary>
	/// <param name="fieldName">Field name</param>
	/// <param name="compareMethod">Type of comparison</param>
	/// <param name="dataType">Data type</param>
	/// <param name="compareData">Compare with</param>
	public Condition(string fieldName, CompareMethod compareMethod, DataType dataType, string compareData)
	{
		Errors      = new();
		FieldName   = fieldName;
		CompareMethod  = compareMethod;
		DataType    = dataType;
		CompareData = compareData;
		Errors      = new();
	}

	/// <summary>
	/// Field name.
	/// </summary>
	/// <example>quantity</example>
	public string FieldName { set; get; } = string.Empty;
	/// <summary>
	/// Type of comparison.
	/// </summary>
	/// <example>eq</example>
	[JsonConverter(typeof(StringEnumConverter))]
	public CompareMethod CompareMethod { set; get; }
	/// <summary>
	/// Data type.
	/// </summary>
	/// <example>1</example>
	[JsonConverter(typeof(StringEnumConverter))]
	public DataType DataType { set; get; }

	/// <summary>
	/// Data to compare with.
	/// </summary>
	/// <example>"Q"</example>
	public object? CompareData { set; get; }

	/// <summary>
	/// Parser errors.
	/// </summary>
	public List<ParserError> Errors { set; get; }

	/// <summary>
	/// Retrieve the compare data as a string.
	/// </summary>
	public string CompareDataAsString()
	{
		return DataType == DataType.StringType ? CompareData?.ToString() ?? string.Empty : string.Empty; 
	}

	/// <summary>
	/// Retrieve the compare data as a double.
	/// </summary>
	public decimal CompareDataAsNumber()
	{
		return DataType == DataType.NumericType ? JsonConvert.DeserializeObject<decimal>(CompareData?.ToString()) : decimal.MinValue;
	}

	/// <summary>
	/// Retrieve the compare data as a date time offset.
	/// </summary>
	public DateTimeOffset CompareDataAsDateTimeOffset()
	{
		var parsed = DateTimeOffset.Parse(CompareData?.ToString() ?? string.Empty).ToString();
		return DataType == DataType.DateType ? DateTimeOffset.Parse(CompareData?.ToString() ?? string.Empty) : DateTimeOffset.MinValue;
	}

	/// <summary>
	/// Retrieve the compare data as a bounding box..
	/// </summary>
	public decimal[] CompareDataAsBoundingBox()
	{
		return DataType == DataType.BboxType ? JsonConvert.DeserializeObject<decimal[]>(CompareData?.ToString()) : new decimal[4];
	}

	/// <summary>
	/// Retrieve the compare data as a list of strings.
	/// </summary>
	public List<string> CompareDataAsArrayOfString()
	{
		return DataType == DataType.ArrayOfStringType ? JsonConvert.DeserializeObject<List<string>>(CompareData?.ToString()) : new List<string>();
	}

	/// <summary>
	/// Retrieve the compare data as a list of double.
	/// </summary>
	public List<decimal> CompareDataAsArrayOfNumber()
	{
		return DataType == DataType.ArrayOfNumericType ? JsonConvert.DeserializeObject<List<decimal>>(CompareData?.ToString()) : new List<decimal>();
	}
}