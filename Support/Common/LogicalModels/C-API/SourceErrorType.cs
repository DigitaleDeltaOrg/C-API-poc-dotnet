using System.Runtime.Serialization;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Source error types.
/// </summary>
public enum SourceErrorType
{
	/// <summary>
	/// 1: Unknown
	/// </summary>
	Unknown = 1,
	/// <summary>
	/// 2: Duplicate
	/// </summary>
	Duplicate = 2,
	/// <summary>
	/// 3: Missing
	/// </summary>
	Missing = 3
}