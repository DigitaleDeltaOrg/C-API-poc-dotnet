namespace Shared.LogicalModels.Source;

/// <summary>
/// Possible authentication types.
/// </summary>
public enum AuthenticationType
{
	/// <summary>
	/// 0: No authentication.
	/// </summary>
	None = 0,
	/// <summary>
	/// 1: Use API keys.
	/// </summary>
	ApiKey = 1,
	/// <summary>
	/// 2: Use OAUTH2 password credentials.
	/// </summary>
	OAuthPasswordTokenRequest = 2,
	/// <summary>
	/// 3: Use OAUTH2 client credentials.
	/// </summary>
	OAuthClientCredentialsTokenRequest = 3,
	/// <summary>
	/// 4: Use OAUTH2 Device code.
	/// </summary>
	OAuthDeviceTokenRequest = 4,
	/// <summary>
	/// 5: Use OAUTH2 authorization code.
	/// </summary>
	OAuthAuthorizationTokenRequest = 5
}