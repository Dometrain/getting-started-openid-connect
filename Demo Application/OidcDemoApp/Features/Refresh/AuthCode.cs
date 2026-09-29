namespace OidcDemoApp.Features.Refresh;

public class AuthCode
{
    public string? Code { get; set; }

    public string? Scope { get; set; }

    public string? State { get; set; }

    public string? SessionState { get; set; }

    public string? Iss { get; set; }

    public string? CodeVerifier { get; set; }
}
