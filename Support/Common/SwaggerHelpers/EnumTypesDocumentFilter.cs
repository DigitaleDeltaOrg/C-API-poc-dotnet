using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.SwaggerHelpers;

// Based on: https://github.com/yakimovim/enum-description-in-swagger/blob/master/EnumTypesDocumentFilter.cs
/// <summary>
/// Handles enum types in the document.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global incorrect assumption of Resharper.
public class EnumTypesDocumentFilter : IDocumentFilter
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="swaggerDoc"></param>
	/// <param name="context"></param>
	public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
	{
		foreach (var path in swaggerDoc.Paths.Values)
		{
			foreach(var operation in path.Operations.Values)
			{
				foreach(var parameter in operation.Parameters)
				{
					ProcessParameter(context, parameter);
				}
			}
		}

	}

	private static void ProcessParameter(DocumentFilterContext context, OpenApiParameter parameter)
	{
		var schemaReferenceId = parameter.Schema.Reference?.Id;
		if (string.IsNullOrEmpty(schemaReferenceId))
		{
			return;
		}

		var schema = context.SchemaRepository.Schemas[schemaReferenceId];
		if (schema.Enum == null || schema.Enum.Count == 0)
		{
			return;
		}

		var cutStart = schema.Description?.IndexOf("<ul>") ?? 0;
		var cutEnd   = schema.Description?.IndexOf("</ul>") + 5 ?? 0;

		parameter.Description += "<p>Variants:</p>" + schema.Description?.Substring(cutStart, cutEnd - cutStart);
	}
}