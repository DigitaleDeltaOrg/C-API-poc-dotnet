using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.LogicalModels.Source;

/// <summary>
/// Defines authentication data for none, API keys or OAuth2. For OAuth2 types see <a href="https://identitymodel.readthedocs.io/en/latest/client/overview.html">https://identitymodel.readthedocs.io/en/latest/client/overview.html</a>.
/// </summary>
public class AuthenticationData
{
	/// <summary>
	/// Type of authentication. For oAuthDeviceRequest, oAuthClientRequest, oAuthAuthRequest, oAuthPasswordRequest see  <a href="https://identitymodel.readthedocs.io/en/latest/client/overview.html">https://identitymodel.readthedocs.io/en/latest/client/overview.html</a>.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public AuthenticationType AuthenticationType { init; get; }
	/// <summary>
	/// API Key data
	/// </summary>
	public ApiKey? ApiKey { init; get; }
	/// <summary>
	/// See  <a href="https://identitymodel.readthedocs.io/en/latest/client/overview.html">https://identitymodel.readthedocs.io/en/latest/client/overview.html</a>
	/// </summary>
	public IdentityModel.Client.DeviceTokenRequest? OAuthDeviceTokenRequest { set; get; }
	/// <summary>
	/// See  <a href="https://identitymodel.readthedocs.io/en/latest/client/overview.html">https://identitymodel.readthedocs.io/en/latest/client/overview.html</a>
	/// </summary>
	public IdentityModel.Client.ClientCredentialsTokenRequest? OAuthClientCredentialsTokenRequest { set; get; }
	/// <summary>
	/// See <a href="https://identitymodel.readthedocs.io/en/latest/client/overview.html">https://identitymodel.readthedocs.io/en/latest/client/overview.html</a>
	/// </summary>
	public IdentityModel.Client.AuthorizationCodeTokenRequest? OAuthAuthorizationCodeTokenRequest { set; get; }
	/// <summary>
	/// See  <a href="https://identitymodel.readthedocs.io/en/latest/client/overview.html">https://identitymodel.readthedocs.io/en/latest/client/overview.html</a>
	/// </summary>
	public IdentityModel.Client.PasswordTokenRequest? OAuthPasswordTokenRequest { init; get; }
}