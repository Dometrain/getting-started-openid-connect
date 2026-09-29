using OidcDemoApp.Models;

namespace OidcDemoApp.Features.ClientCredentials;

public class ClientCredentialRequest
{
    public string? GrantType { get; set; } = "client_credentials";

    public string? ClientId { get; set; } = Settings.ClientCredentialsClientId;

    public string? ClientSecret { get; set; } = Settings.ClientCredentialsClientSecret;

    public string? Scope { get; set; } = "api";

    public OidcTokenResponse? Token { get; set; }

    public string? ApiResult { get; set; }
}