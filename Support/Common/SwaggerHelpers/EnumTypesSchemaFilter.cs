using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// Based on: https://github.com/yakimovim/enum-description-in-swagger/blob/master/EnumTypesSchemaFilter.cs
namespace Shared.SwaggerHelpers;

/// <summary>
/// 
/// </summary>
public class EnumTypesSchemaFilter : ISchemaFilter
{
	private readonly XDocument? _xmlComments;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="xmlPath"></param>
	public EnumTypesSchemaFilter(string xmlPath)
	{
		if(File.Exists(xmlPath))
		{
			_xmlComments = XDocument.Load(xmlPath);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="schema"></param>
	/// <param name="context"></param>
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		if (_xmlComments == null || schema.Enum is not { Count: > 0 } || context.Type is not { IsEnum: true })
		{
			return;
		}

		var fullTypeName = context.Type.FullName;

		foreach (var enumMemberName in schema.Enum.OfType<OpenApiString>().Select(v => v.Value))
		{
			var fullEnumMemberName = $"F:{fullTypeName}.{enumMemberName}";
			var enumMemberComments = _xmlComments.Descendants("member").FirstOrDefault(m => m.Attribute("name")?.Value.Equals(fullEnumMemberName, StringComparison.OrdinalIgnoreCase) ?? false);
			var summary            = enumMemberComments?.Descendants("summary").FirstOrDefault();
			if (summary == null)
			{
				continue;
			}
			
			schema.Description += $"<li><i>{summary.Value.Trim()} -> {enumMemberName}</i></li>";
		}

		schema.Description += "</ul>";
	}
}