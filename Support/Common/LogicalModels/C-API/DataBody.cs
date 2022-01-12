using System;
using System.Collections.Generic;
using Shared.LogicalModels.C_API.Parsing;

namespace Shared.LogicalModels.C_API;

/// <summary>
/// Represents data that needs to be passed to the plug-in. It describes information concerning the source, the query conditions and the Id provided by the connector.
/// </summary>
public class DataBody
{
	/// <summary>
	/// Describes how to access the source, how to authenticate, translate, etc.
	/// </summary>
	public Source.SourceDefinition SourceDefinition { set; get; } = new();

	/// <summary>
	/// Describes the query conditions.
	/// </summary>
	public List<Condition>? Conditions { set; get; } = new();

	/// <summary>
	/// Id provided by the connector, to keep track of the results.
	/// </summary>
	public Guid ResponseId { set; get; }

	/// <summary>
	/// Connector capabilities.
	/// </summary>
	public List<ConnectorCapability> ConnectorCapabilities { set; get; } = new ();
}