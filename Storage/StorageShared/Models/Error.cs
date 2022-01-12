namespace StorageShared.Models;

/// <summary>
/// Generic error.
/// </summary>
public class Error
{
	/// <summary>
	/// Type of error.
	/// </summary>
	public ErrorType ErrorType  { set; get; }
	/// <summary>
	/// Field name in error.
	/// </summary>
	public string?   FieldName  { set; get; }
	/// <summary>
	/// Field value in error.
	/// </summary>
	public string?   FieldValue { set; get; }

	/// <summary>
	/// Empty constructor.
	/// </summary>
	public Error()
	{
			
	}

	/// <summary>
	/// Specific constructor.
	/// </summary>
	/// <param name="errorType">Type of error</param>
	/// <param name="fieldName">Field name</param>
	/// <param name="fieldValue">Field value</param>
	public Error(ErrorType errorType, string? fieldName = null, string? fieldValue = null)
	{
		ErrorType  = errorType;
		FieldName  = fieldName;
		FieldValue = fieldValue;
	}
}