namespace OidcDemoApp.Models;

// The parameters sent to the OIDC/OAuth authorize endpoint (RFC 6749 section 4.1.1,
// OpenID Connect Core section 3.1.2.1). Shared by every flow demo that builds an
// authorize URL: CodeFlow, CodeFlowPkce, Refresh, and ImplicitFlow.
public class AuthorizeRequest
{
    public string? ResponseType { get; set; }
    public string? ClientId { get; set; }
    public string? Scope { get; set; }
    public string? Prompt { get; set; }
    public string? State { get; set; }
    public string? Nonce { get; set; }
    public string? ResponseMode { get; set; }
    public string? RedirectUri { get; set; } = "";

    // PKCE only (Features/CodeFlowPkce): the hashed code verifier.
    public string? CodeChallenge { get; set; }
}
