namespace OidcDemoApp.Features.CombinedFlows;

public class CombinedFlowsViewModel
{
    // Upper half: the token request this client makes in its own name. Editable so the
    // parameters are visible and can be broken on purpose, same as the client
    // credentials flow page.
    public string? GrantType { get; set; } = "client_credentials";
    public string? ClientId { get; set; } = Settings.ClientCredentialsClientId;
    public string? ClientSecret { get; set; } = Settings.ClientCredentialsClientSecret;
    public string? Scope { get; set; } = "payment";

    // Round-trips through a form field, so the token shown on screen is provably the
    // one used when the APIs are called.
    public string? ClientAccessToken { get; set; }

    public string? ClientScopes { get; set; }
    public string? ClientTokenError { get; set; }

    // Lower half: the token the OpenID Connect handler stored at login (SaveTokens).
    // There is no button for this one - it arrived with the sign-in.
    public bool IsUserSignedIn { get; set; }
    public string? UserDisplayName { get; set; }
    public string? UserSubject { get; set; }
    public string? UserRoles { get; set; }
    public string? UserScopes { get; set; }
    public string? UserAccessToken { get; set; }

    // Set only after the Call APIs button has been pressed.
    public bool WasCalled { get; set; }
    public ApiCallResult? ServiceCall { get; set; }
    public ApiCallResult? UserCall { get; set; }
}

public class ApiCallResult
{
    public required string Url { get; init; }
    public required string TokenDescription { get; init; }

    public int? StatusCode { get; init; }
    public string? Body { get; init; }
    public string? Error { get; init; }

    public bool IsSuccess => StatusCode is >= 200 and < 300;
}
