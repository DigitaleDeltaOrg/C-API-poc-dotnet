using System.Collections.Generic;
using System.Linq;
using Shared.LogicalModels.C_API;
using Shared.LogicalModels.C_API.Parsing;

namespace Shared.LogicalModels;

/// <summary>
/// Helper for retrieving information from ConnectorCapabilities.
/// </summary>
public  static class ConnectorCapabilityLookup
{
	/// <summary>
	/// Get the fieldName from the DataCategory. Defaults to an empty string.
	/// </summary>
	/// <param name="connectorCapabilities"></param>
	/// <param name="dataCategory"></param>
	/// <returns></returns>
	public static string GetParameterForCategory(IEnumerable<ConnectorCapability>? connectorCapabilities, DataCategory dataCategory)
	{
		if (connectorCapabilities == null)
		{
			return string.Empty;
		}
		
		return connectorCapabilities.FirstOrDefault(a => a.DataCategory == dataCategory)?.FieldName ?? string.Empty;
	}
	
	/// <summary>
	/// Get the DataCategory for a field name. Defaults to DataCategory.Other.
	/// </summary>
	/// <param name="connectorCapabilities"></param>
	/// <param name="fieldName"></param>
	/// <returns></returns>
	public static DataCategory GetDataCategoryForFieldName(IEnumerable<ConnectorCapability>? connectorCapabilities, string fieldName)
	{
		if (connectorCapabilities == null)
		{
			return DataCategory.Other;
		}

		return connectorCapabilities.FirstOrDefault(a => a.FieldName == fieldName)?.DataCategory ?? DataCategory.Other;
	}

	/// <summary>
	/// Get the DataCategory for a field name. Defaults to DataCategory.Other.
	/// </summary>
	/// <param name="dataCategory"></param>
	/// <returns></returns>
	public static List<DataType> GetDataTypesForDataCategory(DataCategory dataCategory)
	{
		return dataCategory switch
		{
			DataCategory.Other => new() { DataType.Unsupported },
			DataCategory.Quantity => new() { DataType.StringType, DataType.ArrayOfStringType },
			DataCategory.Unit => new() { DataType.StringType, DataType.ArrayOfStringType },
			DataCategory.Value => new() { DataType.NumericType },
			DataCategory.MeasurementObject => new() { DataType.StringType, DataType.ArrayOfStringType },
			DataCategory.Parameter => new() { DataType.StringType, DataType.ArrayOfStringType },
			DataCategory.MeasurementDate => new() { DataType.DateType },
			DataCategory.Compartment => new() { DataType.StringType, DataType.ArrayOfStringType },
			_ => new()
		};
	}

	/// <summary>
	/// Retrieve only the required field names.
	/// </summary>
	/// <param name="connectorCapabilities"></param>
	/// <returns></returns>
	public static List<string> GetRequiredFieldNames(IEnumerable<ConnectorCapability> connectorCapabilities)
	{
		return connectorCapabilities.Where(a => a.Required).Select(a => a.FieldName).Distinct().ToList();
	}
	
	/// <summary>
	/// Retrieve only the fieldnames from the ConnectorCapabilities.
	/// </summary>
	/// <param name="connectorCapabilities"></param>
	/// <returns></returns>
	public static List<string> GetFieldNames(IEnumerable<ConnectorCapability> connectorCapabilities)
	{
		return connectorCapabilities.Select(a => a.FieldName).Distinct().ToList();
	}

	/// <summary>
	/// Retrieve acceptable compare methods for query type.
	/// </summary>
	/// <param name="queryType"></param>
	/// <returns></returns>
	public static List<CompareMethod> GetCompareMethodsForQueryType(QueryType queryType)
	{
		switch (queryType)
		{
			case QueryType.StringExact:
				return new List<CompareMethod> { CompareMethod.Eq, CompareMethod.Ne, CompareMethod.In, CompareMethod.Notin };
			case QueryType.StringWildCard:
				return new List<CompareMethod> { CompareMethod.Eq, CompareMethod.Ne, CompareMethod.In, CompareMethod.Notin, CompareMethod.Like, CompareMethod.Startswith, CompareMethod.Endswith };
			case QueryType.Numeric:
				return new List<CompareMethod> { CompareMethod.Eq, CompareMethod.Ne, CompareMethod.In, CompareMethod.Notin, CompareMethod.Ge, CompareMethod.Gt, CompareMethod.Le, CompareMethod.Lt };
			case QueryType.Date:
				return new List<CompareMethod> { CompareMethod.Eq, CompareMethod.Ne, CompareMethod.Ge, CompareMethod.Gt, CompareMethod.Le, CompareMethod.Lt };
			case QueryType.Geo:
				return new List<CompareMethod> { CompareMethod.Bbox, CompareMethod.Wkt, CompareMethod.GeoJson };
			default:
				return new();
		}
	}

	/// <summary>
	/// Determine the data type for the combination QueryType and CompareMethod
	/// </summary>
	/// <param name="queryType"></param>
	/// <param name="compareMethod"></param>
	/// <returns></returns>
	public static DataType? GetDataTypeForQueryType(QueryType queryType, CompareMethod compareMethod)
	{
		switch (queryType)
		{
			case QueryType.StringExact:
				return HandleString(compareMethod);

			case QueryType.StringWildCard:
				return HandleWildCard(compareMethod);

			case QueryType.Numeric:
				return compareMethod is CompareMethod.Eq or CompareMethod.Ne or CompareMethod.Ge or CompareMethod.Gt or CompareMethod.Le or CompareMethod.Lt ? DataType.NumericType : null;

			case QueryType.Date:
				return compareMethod is CompareMethod.Eq or CompareMethod.Ne or CompareMethod.Ge or CompareMethod.Gt or CompareMethod.Le or CompareMethod.Lt ? DataType.DateType : null;

			case QueryType.Geo:
				return HandleGeo(compareMethod);
		}

		return null;
	}

	private static DataType? HandleGeo(CompareMethod compareMethod)
	{
		return compareMethod switch
		{
			CompareMethod.Wkt or CompareMethod.GeoJson => DataType.StringType,
			CompareMethod.Bbox => DataType.ArrayOfNumericType,
			_ => null
		};
	}

	private static DataType? HandleString(CompareMethod compareMethod)
	{
		return compareMethod switch
		{
			CompareMethod.Eq or CompareMethod.Ne => DataType.StringType,
			CompareMethod.In or CompareMethod.Notin => DataType.ArrayOfStringType,
			_ => null
		};
	}

	private static DataType? HandleWildCard(CompareMethod compareMethod)
	{
		return compareMethod switch
		{
			CompareMethod.Eq or CompareMethod.Ne or CompareMethod.Like or CompareMethod.Startswith or CompareMethod.Endswith => DataType.StringType,
			CompareMethod.In or CompareMethod.Notin => DataType.ArrayOfStringType,
			_ => null
		};
	}
}