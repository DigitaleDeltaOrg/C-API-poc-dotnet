namespace StorageShared.Models;

public enum ErrorType
{
	/// <summary>
	/// Field has a value although it should not have.
	/// </summary>
	FieldHasValue,
	/// <summary>
	/// Field has no value although it should have.
	/// </summary>
	FieldHasNoValue,
	/// <summary>
	/// Invalid value for field.
	/// </summary>
	Invalid,
	/// <summary>
	/// No (timely) response from the storage.
	/// </summary>
	NoResponse,
	/// <summary>
	/// Unknown response.
	/// </summary>
	Unknown,
	/// <summary>
	/// Storage appears not to be initialized.
	/// </summary>
	NotInitialized,
	/// <summary>
	/// Duplicate key.
	/// </summary>
	Duplicate,
	/// <summary>
	/// Error performing the operation.
	/// </summary>
	Failed,
	/// <summary>
	/// Authentication failed.
	/// </summary>
	AuthenticationFailed
}