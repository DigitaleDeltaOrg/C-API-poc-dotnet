namespace Shared.LogicalModels.Source;

/// <summary>
/// Describes a non-measurement endpoint in Z-Info to retrieve entities.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class EndPointFieldNameDescriptionName
{
	/// <summary>
	/// Name of the endpoint (SPCID)
	/// </summary>
	public string EndPoint { set; get; } = string.Empty;
	/// <summary>
	/// Name of the field holding the code of the entity.
	/// </summary>
	public string FieldName { set; get; } = string.Empty;
	/// <summary>
	/// Name of the field holding the description (name) of the entity.
	/// </summary>
	public string DescriptionName { set; get; } = string.Empty;
}