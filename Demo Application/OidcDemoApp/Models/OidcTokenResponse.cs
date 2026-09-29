using System.Text.Json.Serialization;

namespace OidcDemoApp.Models;

// The JSON body returned by the OIDC token endpoint (RFC 6749 section 5.1).
// Not every flow returns every field, for example the client credentials flow
// never returns an id_token or refresh_token.
public class OidcTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
