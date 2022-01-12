using System.Collections.Generic;

namespace StorageShared.Models;

/// <summary>
/// A CUD response (Create Update Delete Response) is a generic class to retrieve responses from a Create, Update or Delete action.
/// </summary>
/// <typeparam name="T">Generic entity</typeparam>
public class CudResponse<T> where T: class, new()
{
	public CudResponse(T? model)
	{
		Model   = model;
		IsValid = true;
	}
		
	public T?          Model   { get; }
	public bool        IsValid { private set; get; }
	public List<Error> Errors  { get; } = new();

	public void AddError(ErrorType errorType, string? fieldName = null, string? fieldValue = null)
	{
		Errors.Add(new() { ErrorType = errorType, FieldName = fieldName, FieldValue = fieldValue});
		IsValid = false;
	}
}