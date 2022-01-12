using Shared.LogicalModels.C_API.Parsing;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Defines a field definition.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class FieldDefinition
{
	/// <summary>
	/// Constructs a FieldDefinition
	/// </summary>
	/// <param name="fieldName">Field name</param>
	/// <param name="description">Field description</param>
	/// <param name="compareMethod">Compare method</param>
	/// <param name="dataType">Data type</param>
	/// <param name="dataCategory">Data category</param>
	/// <param name="required">Required</param>
	public FieldDefinition(string fieldName, string description, CompareMethod compareMethod, DataType dataType, DataCategory dataCategory = DataCategory.Other, bool required = false)
	{
		FieldName     = fieldName;
		CompareMethod = compareMethod;
		DataType      = dataType;
		Required      = required;
		Description   = description;
		DataCategory  = dataCategory;
	}

	/// <summary>
	/// Field name.
	/// </summary>
	/// <example>quantity</example>
	public string FieldName { get; }
	/// <summary>
	/// Compare method.
	/// </summary>
	/// <example>eq</example>
	public CompareMethod CompareMethod { get; }
	/// <summary>
	/// Data type.
	/// </summary>
	/// <example>string</example>
	public DataType DataType { get; }
	/// <summary>
	/// Required.
	/// </summary>
	public bool Required { get; }
	/// <summary>
	/// Description.
	/// </summary>
	public string Description { get; }
	/// <summary>
	/// Data category
	/// </summary>
	/// <example>quantity</example>
	public DataCategory DataCategory { get; }
}