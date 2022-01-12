using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Shared.LogicalModels.Source;

namespace PluginShared;

public static class LogonService
{
	public static async Task<(HttpStatusCode statusCode, string? token)> AuthenticateAsync(SourceDefinition sourceDefinition)
	{
		switch (sourceDefinition.AuthenticationData!.AuthenticationType)
		{
			case AuthenticationType.None: // No need to test.
			{
				return (HttpStatusCode.OK, string.Empty);
			}
			case AuthenticationType.ApiKey: // Can only test when making an actual request.
			{
				return (HttpStatusCode.OK, string.Empty);
			}
			case AuthenticationType.OAuthPasswordTokenRequest:
			{
				var client   = new HttpClient();
				var response = await client.RequestPasswordTokenAsync(sourceDefinition.AuthenticationData!.OAuthPasswordTokenRequest).ConfigureAwait(false);
				return response.AccessToken == null ? (HttpStatusCode.Unauthorized, string.Empty) : (HttpStatusCode.OK, response.AccessToken);
			}
			case AuthenticationType.OAuthClientCredentialsTokenRequest:
			{
				var client   = new HttpClient();
				var response = await client.RequestClientCredentialsTokenAsync(sourceDefinition.AuthenticationData!.OAuthClientCredentialsTokenRequest).ConfigureAwait(false);
				return response.AccessToken == null ? (HttpStatusCode.Unauthorized, string.Empty) : (HttpStatusCode.OK, response.AccessToken);
			}
			case AuthenticationType.OAuthDeviceTokenRequest:
			{
				var client   = new HttpClient();
				var response = await client.RequestDeviceTokenAsync(sourceDefinition.AuthenticationData!.OAuthDeviceTokenRequest).ConfigureAwait(false);
				return response.AccessToken == null ? (HttpStatusCode.Unauthorized, string.Empty) : (HttpStatusCode.OK, response.AccessToken);
			}
			case AuthenticationType.OAuthAuthorizationTokenRequest:
			{
				var client   = new HttpClient();
				var response = await client.RequestAuthorizationCodeTokenAsync(sourceDefinition.AuthenticationData!.OAuthAuthorizationCodeTokenRequest).ConfigureAwait(false);
				return response.AccessToken == null ? (HttpStatusCode.Unauthorized, string.Empty) : (HttpStatusCode.OK, response.AccessToken);
			}
			default:
				return (HttpStatusCode.Unauthorized, null);
		}
	}

	public static void HandlePostLogin(SourceDefinition sourceDefinition, HttpClient client, string? token)
	{
		if (sourceDefinition.AuthenticationData == null)
		{
			return;
		}
		
		if (sourceDefinition.AuthenticationData.AuthenticationType == AuthenticationType.ApiKey && sourceDefinition.AuthenticationData != null)
		{
			client.DefaultRequestHeaders.Add(sourceDefinition.AuthenticationData.ApiKey!.Header, sourceDefinition.AuthenticationData.ApiKey.Key);
		}
		
		if (sourceDefinition.AuthenticationData?.AuthenticationType != AuthenticationType.None && sourceDefinition.AuthenticationData?.AuthenticationType != AuthenticationType.ApiKey && token != null)
		{
			client.SetBearerToken(token);
		}
	}
}