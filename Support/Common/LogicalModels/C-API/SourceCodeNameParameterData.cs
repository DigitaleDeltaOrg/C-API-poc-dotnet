namespace Shared.LogicalModels.C_API;

/// <summary>
/// 
/// </summary>
public class SourceCodeNameParameterData : SourceCodeName
{
	/// <summary>
	/// Type of parameter.
	/// </summary>
	public string? ParameterType { set; get; }
	/// <summary>
	/// Taxon type of parameter, if ParameterType = Taxon
	/// </summary>
	public string? TaxonType     { set; get; }
	/// <summary>
	/// Taxon group of parameter, if ParameterType = Taxon
	/// </summary>
	public string? TaxonGroup    { set; get; }
	/// <summary>
	/// Author(s) of parameter, if ParameterType = Taxon
	/// </summary>
	public string? Authors       { set; get; }
}