using System.Text.Json.Serialization;

namespace OidcDemoApp.Features.CodeFlow;

public class AuthCode
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("session_state")]
    public string? SessionState { get; set; }

    [JsonPropertyName("iss")]
    public string? Iss { get; set; }
}
