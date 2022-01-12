using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoLibrary.IO.Wkt;
using Newtonsoft.Json;
using Shared.LogicalModels;
using Shared.LogicalModels.C_API;
using Shared.LogicalModels.C_API.Parsing;

namespace Shared;

/// <summary>
///   Main filter class. Parses a line into separate condition, and condition into field name, operator and value
///   components.
/// </summary>
public sealed class FilterParser
{
	#region Public
	/// <summary>
	/// Get the validity of the parser
	/// </summary>
	/// <returns>true if valid, false otherwise</returns>
	public bool IsValid() => !Errors.Any();

	/// <summary>
	/// Get the errors
	/// </summary>
	/// <returns>List of Error</returns>
	public List<ParserError> GetErrors() => Errors;

	/// <summary>
	/// Get the parsed condition
	/// </summary>
	/// <returns>List of parsed Condition</returns>
	public List<Condition> GetConditions() => Conditions;

	/// <summary>
	///   Parse the specified line.
	/// </summary>
	/// <param name="filter">Filter to be parsed.</param>
	/// <param name="allowEmptyFilter"></param>
	/// <param name="connectorCapabilities"></param>
	/// <param name="skipUnknownFields">Skip unknown fieldDefinitions</param>
	public void Parse(string? filter, bool allowEmptyFilter, List<ConnectorCapability> connectorCapabilities, bool skipUnknownFields = false)
	{
		Initialize();
		ValidateFilterNotNull(filter, allowEmptyFilter);
		ValidateQuotes(filter);
		ValidateBrackets(filter);
		ValidateStatements(filter, connectorCapabilities, skipUnknownFields);
		ValidateRequiredFields(connectorCapabilities);
	}
	#endregion
		
	#region Private
	private const char              ComponentSeparator = ':';
	private const char              ComparerSeparator  = ';';
	private       List<Condition>   Conditions { get; } = new();
	private       List<ParserError> Errors     { get; } = new();


	/// <summary>
	/// Initialize condition and errors
	/// </summary>
	private void Initialize()
	{
		Conditions.Clear();
		Errors.Clear();
	}

	/// <summary>
	/// Validate nullity of error
	/// </summary>
	/// <param name="filter">Provider filter</param>
	/// <param name="allowEmptyFilter">Allow empty filter?</param>
	private void ValidateFilterNotNull(string? filter, bool allowEmptyFilter)
	{
		if (!IsValid())
		{
			return;
		}
			
		if (!allowEmptyFilter && string.IsNullOrWhiteSpace(filter))
		{
			Errors.Add(new(ErrorType.FilterRequired, string.Empty));
		}
	}

	/// <summary>
	/// Validate the statements of the provided filter
	/// </summary>
	/// <param name="filter">Provided filter</param>
	/// <param name="connectorCapabilities"></param>
	/// <param name="skipUnknownFields">Fields to skip</param>
	private void ValidateStatements(string? filter, List<ConnectorCapability> connectorCapabilities, bool skipUnknownFields)
	{
		if (!IsValid())
		{
			return;
		}

		var filterParts = SplitLine(filter!, ComparerSeparator).ToList();
		filterParts.ForEach(a => ParseStatement(a, connectorCapabilities, skipUnknownFields));
	}

	/// <summary>
	/// Get Comparer from string
	/// </summary>
	/// <param name="operator">name of the operator</param>
	/// <returns></returns>
	private static CompareMethod ParseOperator(string @operator)
	{
		return @operator.ToLowerInvariant() switch
		{
			"eq"         => CompareMethod.Eq,
			"ne"         => CompareMethod.Ne,
			"lt"         => CompareMethod.Lt,
			"le"         => CompareMethod.Le,
			"ge"         => CompareMethod.Ge,
			"gt"         => CompareMethod.Gt,
			"in"         => CompareMethod.In,
			"notin"      => CompareMethod.Notin,
			"like"       => CompareMethod.Like,
			"startswith" => CompareMethod.Startswith,
			"endswith"   => CompareMethod.Endswith,
			"wkt"        => CompareMethod.Wkt,
			"geojson"    => CompareMethod.GeoJson,
			"bbox"       => CompareMethod.Bbox,
			_            => CompareMethod.Unknown
		};
	}
		
	/// <summary>
	/// Check if the brackets are OK (by using counts) in the filter
	/// </summary>
	/// <param name="filter">Provided filter</param>
	private void ValidateBrackets(string? filter)
	{
		if (!IsValid() || filter == null)
		{
			return;
		}
			
		var openingBracketCount = filter.Count(a => a == '[');
		var closingBracketCount = filter.Count(a => a == ']');
		if (openingBracketCount != closingBracketCount)
		{
			Errors.Add(new(ErrorType.BracketMismatch, filter));
		}
	}

	/// <summary>
	/// Validate quotes int  the filter
	/// </summary>
	/// <param name="filter">Provided filter</param>
	private void ValidateQuotes(string? filter)
	{
		if (!IsValid() || filter == null)
		{
			return;
		}
			
		var singleQuoteCount = filter.Count(a => a == '\'');
		if (singleQuoteCount % 2 != 0)
		{
			Errors.Add(new(ErrorType.QuoteMismatch, "'"));
		}

		var doubleQuoteCount = filter.Count(a => a == '"');
		if (doubleQuoteCount % 2 != 0)
		{
			Errors.Add(new(ErrorType.QuoteMismatch, "\""));
		}
	}

	/// <summary>
	/// Validate if required fieldDefinitions are provided
	/// </summary>
	/// <param name="connectorCapabilities"></param>
	private void ValidateRequiredFields(IEnumerable<ConnectorCapability> connectorCapabilities)
	{
		if (!IsValid())
		{
			return;
		}

		var fieldNames = Conditions.Select(a => a.FieldName).Distinct().ToList();
		var requiredFieldNames = ConnectorCapabilityLookup.GetRequiredFieldNames(connectorCapabilities);
		if (!requiredFieldNames.Any())
		{
			return;
		}
		
		var missingFieldNames = requiredFieldNames.Except(fieldNames).ToList();
		foreach (var missingFieldName in missingFieldNames)
		{
			Errors.Add(new(ErrorType.FilterRequired, missingFieldName));
		}
	}

	/// <summary>
	///   Splits a test into parts, specified by the separator. Takes into account that the separator can be embedded in
	///   quotes
	/// </summary>
	/// <param name="stringToSplit">The string to be split</param>
	/// <param name="separator">The separator character</param>
	/// <param name="allowQuoted">Allow quoted?</param>
	/// <returns>List of string.</returns>
	// <remarks>Improved and modernized version of: https://stackoverflow.com/a/31804981 </remarks>
	private static List<string> SplitLine(string stringToSplit, char separator, bool allowQuoted = false)
	{
		if (!allowQuoted)
		{
			return stringToSplit.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries).
				ToList();
		}

		var characters      = stringToSplit.ToCharArray();
		var returnValueList = new List<string>();
		var tempString      = new StringBuilder();
		var inDoubleQuote   = false;
		var inSingleQuote   = false;
		var characterCount  = 0;
		var length          = characters.Length;
			
		foreach (var character in characters)
		{
			characterCount++;
			ProcessCharacter(separator, characterCount, character, tempString, returnValueList, length, ref inSingleQuote, ref inDoubleQuote);
		}

		return returnValueList;
	}

	/// <summary>
	/// Process a single character in the complete filter
	/// </summary>
	/// <param name="separator">Separator character</param>
	/// <param name="characterCount">Current character index</param>
	/// <param name="character">Current character</param>
	/// <param name="tempString">Builder for part</param>
	/// <param name="returnValueList">List of parsed parts</param>
	/// <param name="length">Total string length</param>
	/// <param name="inSingleQuote">In single quote block?</param>
	/// <param name="inDoubleQuote">In double quote block?</param>
	private static void ProcessCharacter(char separator, int characterCount, char character, StringBuilder tempString, List<string> returnValueList, int length, ref bool inSingleQuote, ref bool inDoubleQuote)
	{
		switch (character) // Handle quotes.
		{
			case '"' when !inSingleQuote:
				inDoubleQuote = !inDoubleQuote;
				break;

			case '\'' when !inDoubleQuote:
				inSingleQuote = !inSingleQuote;
				break;
		}

		if (character != separator) // If not separator
		{
			tempString.Append(character); // Add the character
		}
		else
		{
			if (inDoubleQuote || inSingleQuote)
			{
				tempString.Append(character);
			}
			else
			{
				returnValueList.Add(tempString.ToString());
				tempString.Clear();
			}	
		}

		if (characterCount != length)
		{
			return;
		}

		returnValueList.Add(tempString.ToString());
		tempString.Clear();
	}

	/// <summary>
	///   Parses a statement into components
	/// </summary>
	/// <param name="statement">Statement (part of the provided filter)</param>
	/// <param name="connectorCapabilities"></param>
	/// <param name="skipUnknownFields">Skip unknown fieldDefinitions?</param>
	private void ParseStatement(string statement, List<ConnectorCapability> connectorCapabilities, bool skipUnknownFields)
	{
		if (!IsValid())
		{
			return;
		}

		var condition = new Condition();
		var parts     = GetParts(statement);
			
		if (!IsValid())
		{
			return;
		}
			
		// The parts are: part[0]: parameter name, part[1]: operator, part[2]: value
		condition.FieldName     = parts[0].ToLowerInvariant().Trim();
		condition.CompareMethod = ParseOperator(parts[1].ToUpperInvariant());
		condition.DataType      = GetDataTypeForStatement(condition, connectorCapabilities);
		condition.CompareData   = parts[2];

		ValidateComparer(statement, condition);
		ValidateArray(statement, condition, parts);
		ValidateFieldNames(condition, connectorCapabilities, skipUnknownFields);
		ValidateDataType(statement, connectorCapabilities, condition, skipUnknownFields);
		ValidateValue(condition);

		if (IsValid())
		{
			Conditions.Add(condition);
		}
	}

	private static DataType GetDataTypeForStatement(Condition condition, List<ConnectorCapability> connectorCapabilities)
	{
		var capabilitiesForField = connectorCapabilities.Where(a => a.FieldName == condition.FieldName).ToList();
		if (!capabilitiesForField.Any())
		{
			return DataType.Unsupported;
		}
		
		foreach (var capabilityForField in capabilitiesForField)
		{
			// Get allowed compare methods for the field.
			var dataType = ConnectorCapabilityLookup.GetDataTypeForQueryType(capabilityForField.QueryType, condition.CompareMethod);
			if (dataType != null)
			{
				return dataType.Value;
			}
		}

		return DataType.Unsupported;
	}

	/// <summary>
	/// Validate data type
	/// </summary>
	/// <param name="statement"></param>
	/// <param name="connectorCapabilities"></param>
	/// <param name="condition"></param>
	/// <param name="skipUnknownFields"></param>
	private void ValidateDataType(string statement, IReadOnlyCollection<ConnectorCapability> connectorCapabilities, Condition condition, bool skipUnknownFields)
	{
		if (!IsValid())
		{
			return;
		}

		if (skipUnknownFields && !ConnectorCapabilityLookup.GetFieldNames(connectorCapabilities).Contains(condition.FieldName))
		{
			return;
		}

		var dataCategory                    = ConnectorCapabilityLookup.GetDataCategoryForFieldName(connectorCapabilities, condition.FieldName);
		var allowedDataTypesForDataCategory = ConnectorCapabilityLookup.GetDataTypesForDataCategory(dataCategory);
		if (!allowedDataTypesForDataCategory.Contains(condition.DataType))
		{
			Errors.Add(new(ErrorType.InvalidDataType, statement));
		}
	}

	/// <summary>
	/// Validate field names
	/// </summary>
	/// <param name="condition"></param>
	/// <param name="connectorCapabilities"></param>
	/// <param name="skipUnknownFields"></param>
	private void ValidateFieldNames(Condition condition, IEnumerable<ConnectorCapability> connectorCapabilities, bool skipUnknownFields)
	{
		if (!IsValid())
		{
			return;
		}

		if (skipUnknownFields)
		{
			return;
		}
			
		var knownFieldNames   = ConnectorCapabilityLookup.GetFieldNames(connectorCapabilities).ToList();
		if (!knownFieldNames.Contains(condition.FieldName))
		{
			Errors.Add(new(ErrorType.UnknownField, condition.FieldName));
		}
	}

	/// <summary>
	/// Validate comparer
	/// </summary>
	/// <param name="statement"></param>
	/// <param name="condition"></param>
	private void ValidateComparer(string statement, Condition condition)
	{
		if (!IsValid())
		{
			return;
		}
			
		if (condition.CompareMethod == CompareMethod.Unknown)
		{
			Errors.Add(new(ErrorType.UnknownCompareMethod, statement));
		}
	}
	
	/// <summary>
	/// Get the parts of the filter
	/// </summary>
	/// <param name="statement"></param>
	/// <returns></returns>
	private List<string> GetParts(string statement)
	{
		var parts = SplitLine(statement, ComponentSeparator); // Fast parser.
		if (parts.Count != 3)
		{
			// Everything OK.
			parts = SplitLine(statement, ComponentSeparator, true); // Retry with the slow (embed-enabled) parser.
		}

		if (parts.Count != 3 || string.IsNullOrEmpty(parts[2])) // Not three parts. Special case.
		{
			Errors.Add(new(ErrorType.InvalidSyntax, statement));
		}

		return parts;
	}

	/// <summary>
	/// Validate array
	/// </summary>
	/// <param name="statement"></param>
	/// <param name="condition"></param>
	/// <param name="parts"></param>
	private void ValidateArray(string statement, Condition condition, IReadOnlyList<string> parts)
	{
		if ((condition.CompareMethod != CompareMethod.In && condition.CompareMethod != CompareMethod.Notin))
		{
			return;
		}
			
		if (!parts[2].Contains('[') || !parts[2].Contains(']'))
		{
			Errors.Add(new(ErrorType.ArrayNotSpecified, statement));
			return;
		}

		var raw = parts[2].Replace("[",  "").Replace("]",  "").Replace("\"", "").Replace("'",  "").Trim();
		if (raw == string.Empty)
		{
			Errors.Add(new(ErrorType.ArrayIsEmpty, statement));
			return;
		}

		if ((condition.CompareMethod == CompareMethod.In || condition.CompareMethod == CompareMethod.Notin) && HasListDuplicates(raw.Split(",").ToList()))
		{
			Errors.Add(new(ErrorType.ArrayContainsDuplicates, statement));
		}
	}

	/// <summary>
	/// Determine if the list contains duplicates
	/// </summary>
	/// <param name="list"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	private static bool HasListDuplicates<T>(IReadOnlyCollection<T> list)
	{
		return list.Count != list.Distinct().Count();
	}

	/// <summary>
	///   Parses a value, based on data type
	/// </summary>
	/// <param name="condition">The processed statement</param>
	/// <returns>Error type</returns>
	/// <remarks>Uses the Newtonsoft JsonConvertor to parse the value.</remarks>
	private void ValidateValue(Condition condition)
	{
		var compareData = condition.CompareData?.ToString();
		try
		{
			switch (condition.DataType)
			{
				case DataType.StringType:
					ProcessStringType(compareData, condition);
					break;

				case DataType.DateType:
					ProcessDateType(compareData, condition);
					break;

				case DataType.NumericType:
					ProcessNumericType(compareData, condition);
					break;

				case DataType.ArrayOfStringType:
					ProcessArrayOfStringType(compareData, condition);
					break;

				case DataType.ArrayOfNumericType:
					ProcessArrayOfNumericType(compareData, condition);
					break;

				case DataType.BboxType:
					ProcessBboxType(compareData, condition);
					break;

				case DataType.PolygonType:
					ProcessPolygonType(compareData, condition);
					break;

				case DataType.WktType:
					ProcessWktType(compareData, condition);
					break;

				case DataType.Unsupported:
					break;

				default:
					Errors.Add(new(ErrorType.InvalidDataType, condition.DataType.ToString()));
					break;
			}
		}
		catch (Exception)
		{
			Errors.Add(new (ErrorType.InvalidValue, JsonConvert.SerializeObject(condition.CompareData)));
		}
	}

	/// <summary>
	/// Process WKT type. Use WktReader to check validity
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessWktType(string? value, Condition condition)
	{
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}
		
		condition.CompareData = string.Empty;
		condition.DataType    = DataType.WktType;

		if ((!value.StartsWith("'") && !value.EndsWith("'") && (!value.StartsWith("\"") && !value.EndsWith("\""))))
		{
			Errors.Add(new (ErrorType.NotQuoted, value));
			return;
		}

		try
		{
			var result = JsonConvert.DeserializeObject<string>(value);
			WktReader.Read(result);
			condition.CompareData = result;
		}
		catch (Exception)
		{
			Errors.Add(new (ErrorType.InvalidWkt, value));
		}
	}
		
	/// <summary>
	/// Process polygon type
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessPolygonType(string? value, Condition condition)
	{
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}
		
		condition.CompareData = string.Empty;
		condition.DataType    = DataType.PolygonType;
		var result = JsonConvert.DeserializeObject<List<decimal>>(value);
		if (result == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, value));
			return;
		}

		if (result.Count % 2 != 0)
		{
			Errors.Add(new (ErrorType.InvalidPolygon, value));
			return;
		}

		for (var i = 0; i < result.Count; i += 2)
		{
			if (!result[i].
				    IsBetween(-180, 180) || !result[i + 1].
				    IsBetween(-90, 90))
			{
				Errors.Add(new (ErrorType.InvalidBbox, value));
			}
		}

		condition.CompareData = JsonConvert.SerializeObject(result);
	}

	/// <summary>
	/// Process bounding box type
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessBboxType(string? value, Condition condition)
	{
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}
		
		condition.CompareData = string.Empty;
		condition.DataType    = DataType.BboxType;
		var result = JsonConvert.DeserializeObject<List<decimal>>(value);
		if (result == null)
		{
			Errors.Add(new (ErrorType.NotQuoted, value));
			return;
		}

		if (result.Count != 4)
		{
			Errors.Add(new (ErrorType.InvalidBbox, value));
			return;
		}

		if (!result[0].IsBetween(-180, 180)      || // Fist X
		    !result[1].IsBetween(-90, 90)        || // First Y
		    !result[2].IsBetween(result[0], 180) || // Larger than first X
		    !result[3].IsBetween(result[1], 90))    // Larger than first Y
		{
			Errors.Add(new (ErrorType.NotQuoted, value));
			return;
		}

		condition.CompareData = JsonConvert.SerializeObject(result);
	}

	/// <summary>
	/// Process an array of numeric type
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessArrayOfNumericType(string? value, Condition condition)
	{
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}

		var result = JsonConvert.DeserializeObject<List<decimal>>(value);
		if (result == null)
		{
			Errors.Add(new (ErrorType.NotQuoted, value));
			return;
		}

		condition.CompareData = JsonConvert.SerializeObject(result);
		condition.DataType    = DataType.ArrayOfNumericType;
		if (HasListDuplicates(result))
		{
			Errors.Add(new (ErrorType.ArrayContainsDuplicates, value));
		}
	}

	/// <summary>
	/// Process an array of string type
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessArrayOfStringType(string? value, Condition condition)
	{
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}
		
		var parts = value.Split(",");
		foreach (var part in parts)
		{
			if ((!part.StartsWith("'") && !part.EndsWith("'") && (!part.StartsWith("\"") && !part.EndsWith("\""))))
			{
				Errors.Add(new (ErrorType.NotQuoted, value));
			}
		}
			
		var result = JsonConvert.DeserializeObject<List<string>>(value);
		if (result == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, value));
			return;
		}

		condition.CompareData = JsonConvert.SerializeObject(result);
		condition.DataType    = DataType.ArrayOfStringType;
		if (result.Count == 0)
		{
			Errors.Add(new (ErrorType.ArrayIsEmpty, value));
			return;
		}

		if (HasListDuplicates(result))
		{
			Errors.Add(new (ErrorType.ArrayContainsDuplicates, value));
		}
	}

	/// <summary>
	/// Process a numeric type
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessNumericType(string? value, Condition condition)
	{
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}
		
		var result = JsonConvert.DeserializeObject<decimal>(value);
		condition.CompareData = JsonConvert.SerializeObject(result);
		condition.DataType    = DataType.NumericType;
	}

	/// <summary>
	/// Process a date type
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessDateType(string? value, Condition condition)
	{
		if (condition.DataType != DataType.DateType)
		{
			return;
		}
		
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}

		var dateString = JsonConvert.DeserializeObject<string>(value);
		if (!DateTime.TryParse(dateString, out var result))
		{
			Errors.Add(new (ErrorType.InvalidDataType, value));
			return;
		}

		condition.CompareData = dateString;
		condition.DataType    = DataType.DateType;
	}

	/// <summary>
	/// Process a string type
	/// </summary>
	/// <param name="value"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	private void ProcessStringType(string? value, Condition condition)
	{
		if (value == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, condition.FieldName));
			return;
		}

		if ((!value.StartsWith("'") && !value.EndsWith("'") && (!value.StartsWith("\"") && !value.EndsWith("\""))))
		{ 
			Errors.Add(new (ErrorType.NotQuoted, value));
			return;
		}
			
		var result = JsonConvert.DeserializeObject<string>(value);
		if (result == null)
		{
			Errors.Add(new (ErrorType.InvalidValue, value));
			return;
		}

		condition.CompareData = result;
		condition.DataType    = DataType.StringType;
	}
	#endregion
}