namespace NativeApplication;

/// <summary>
/// Every value here points at the instructor's hosted authorization server and demo
/// API, the same ones the web based demo application uses. Nothing below is a real
/// secret: these values are demo only and are meant to be visible to students.
/// </summary>
public static class Settings
{
    // The authorization server (Duende IdentityServer).
    public const string Authority = "https://identityservice.secure.nu";

    // We deliberately reuse the demo application's existing authorization code flow
    // client, so this console application works against the hosted authorization
    // server without registering anything new.
    public const string ClientId = "codeflowclient";
    public const string ClientSecret = "mysecret";

    // The redirect URI already registered for that client. It decides everything else
    // in this demo: the port we listen on (5001), the path we listen for
    // (/codeflow/callback) and the fact that the listener has to speak HTTPS.
    //
    // A real native application would register its own loopback redirect URI, and the
    // usual choice is plain HTTP on 127.0.0.1 with a random free port, exactly as
    // described in BCP 212 (RFC 8252, OAuth 2.0 for Native Apps). Here we borrow an
    // existing registration instead, so there is nothing to configure before running.
    public const string RedirectUri = "https://localhost:5001/codeflow/callback";

    // What we ask for:
    //   openid          identifies this as an OpenID Connect request and returns an ID token
    //   profile         name and other profile claims
    //   payment         required by the payment API endpoint we call at the end
    //   offline_access  asks for a refresh token
    public const string Scope = "openid profile payment offline_access";

    // The protected payment API. Its two endpoints ask different things of the token:
    //   /identity   any authenticated caller, echoes back every claim in the token
    //   /payments   requires scope=payment and role=finance (users alice and bob)
    //
    // Signing in as the guest user is therefore interesting: /identity answers, and
    // /payments refuses with 403 Forbidden.
    public const string PaymentApiIdentityEndpoint = "https://paymentapi.secure.nu/identity";
    public const string PaymentApiPaymentsEndpoint = "https://paymentapi.secure.nu/payments";
}
