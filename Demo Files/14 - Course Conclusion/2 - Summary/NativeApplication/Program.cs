using Microsoft.AspNetCore.WebUtilities;
using NativeApplication;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// -----------------------------------------------------------------------------------
//  Native application demo: interactive login from a console application
// -----------------------------------------------------------------------------------
//
//  A console application has no login page and no browser. It also must never be
//  trusted with the user's password. So how does it sign a user in?
//
//  It borrows the system browser, and it gets the answer back over loopback:
//
//      console app  ->  opens the system browser
//      browser      ->  authorization server, the user signs in there
//      browser      ->  redirected to https://localhost:5001/codeflow/callback?code=...
//      console app  ->  is listening on that address, so it receives the code
//      console app  ->  exchanges the code for tokens on the back channel
//      console app  ->  calls the payment API with the access token
//
//  This is the Authorization Code Flow with PKCE, the flow every native application
//  should use. See BCP 212 (RFC 8252, OAuth 2.0 for Native Apps).
//
//  Note: this demo reuses the web demo application's client registration, so it has to
//  listen on port 5001. Stop the web demo application before running this one, or the
//  two will fight over the port.
// -----------------------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;

Banner("Native Application - Authorization Code Flow with PKCE");

using var http = new HttpClient();


// -----------------------------------------------------------------------------------
// Step 1: Ask the authorization server where its endpoints are
// -----------------------------------------------------------------------------------
// The discovery document is the authorization server's own description of itself. We
// could hardcode /connect/authorize and /connect/token, but reading them from
// discovery is what a real client does, and it keeps working if the server moves them.

Step(1, "Reading the discovery document");

var discovery = await http.GetFromJsonAsync<JsonElement>(
    $"{Settings.Authority}/.well-known/openid-configuration");

var authorizeEndpoint = discovery.GetProperty("authorization_endpoint").GetString()!;
var tokenEndpoint = discovery.GetProperty("token_endpoint").GetString()!;

Info("issuer                ", discovery.GetProperty("issuer").GetString()!);
Info("authorization_endpoint", authorizeEndpoint);
Info("token_endpoint        ", tokenEndpoint);


// -----------------------------------------------------------------------------------
// Step 2: Create the one time values this login will use
// -----------------------------------------------------------------------------------
// code_verifier / code_challenge  PKCE. The verifier never leaves this process until
//                                 the token request. Only its SHA-256 hash travels
//                                 through the browser. Anyone who steals the
//                                 authorization code out of the browser cannot redeem
//                                 it, because they cannot produce the verifier.
//
// state                           Binds the response we get back to the request we
//                                 sent. Protects against cross site request forgery.
//
// nonce                           Binds the ID token to this specific authentication
//                                 request. We check it again in step 6.

Step(2, "Creating the PKCE values, state and nonce");

var codeVerifier = Base64Url(RandomNumberGenerator.GetBytes(32));
var codeChallenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
var state = Base64Url(RandomNumberGenerator.GetBytes(16));
var nonce = Base64Url(RandomNumberGenerator.GetBytes(16));

Info("code_verifier         ", codeVerifier);
Info("code_challenge        ", codeChallenge);
Info("state                 ", state);
Info("nonce                 ", nonce);


// -----------------------------------------------------------------------------------
// Step 3: Start listening for the redirect, before opening the browser
// -----------------------------------------------------------------------------------
// The order matters. If the browser is opened first and the user signs in quickly, the
// redirect could arrive at a port where nobody is listening yet.

Step(3, "Starting the local listener");

await using var listener = await LoopbackListener.StartAsync(Settings.RedirectUri);

Info("listening on          ", Settings.RedirectUri);


// -----------------------------------------------------------------------------------
// Step 4: Send the user to the authorization server, in the system browser
// -----------------------------------------------------------------------------------
// Everything here travels through the browser, inside the URL. This is the front
// channel, so it carries no secret. The client secret and the code verifier stay here.

Step(4, "Opening the system browser");

var authorizeUrl = QueryHelpers.AddQueryString(authorizeEndpoint, new Dictionary<string, string?>
{
    ["response_type"] = "code",                  // We want an authorization code back
    ["client_id"] = Settings.ClientId,           // Who is asking
    ["redirect_uri"] = Settings.RedirectUri,     // Where to send the browser afterwards
    ["scope"] = Settings.Scope,                  // What we are asking for
    ["state"] = state,                           // Checked when the browser comes back
    ["nonce"] = nonce,                           // Checked inside the ID token
    ["code_challenge"] = codeChallenge,          // PKCE, the hash of our verifier
    ["code_challenge_method"] = "S256",          // Which hash we used

    // How we want the response delivered. With query the browser is redirected back to
    // us and the code arrives in the URL. This is what RFC 8252 describes for native
    // applications using a loopback redirect, and it is what we use here.
    //
    // The other option is form_post, where the authorization server returns a small HTML
    // form that the browser submits to us, putting the code in the request body instead.
    // That is what the web demo application uses, so the two demos show both modes.
    //
    // Both are allowed. OAuth 2.1 forbids tokens in a URL, not authorization codes, and
    // PKCE is what makes a code in a URL safe: without the verifier, a code that leaks
    // through browser history or a log cannot be redeemed by anyone.
    ["response_mode"] = "query"
});

Info("authorize URL         ", authorizeUrl);

OpenBrowser(authorizeUrl);


// -----------------------------------------------------------------------------------
// Step 5: Wait for the browser to come back with the authorization code
// -----------------------------------------------------------------------------------

Step(5, "Waiting for the user to sign in");

var callback = await listener.WaitForCallbackAsync(TimeSpan.FromMinutes(3));

// The user can also decline, or the authorization server can refuse the request. Then
// there is no code, only an error, and it arrives the very same way.
if (callback["error"].FirstOrDefault() is { } error)
{
    Fail($"The authorization server returned an error: {error} {callback["error_description"].FirstOrDefault()}");
    return;
}

// Never skip this check. If the state coming back is not the state we sent, then this
// response belongs to some other request and has to be thrown away.
if (callback["state"].FirstOrDefault() != state)
{
    Fail("The returned state does not match the state we sent. Discarding the response.");
    return;
}

var authorizationCode = callback["code"].FirstOrDefault();

if (string.IsNullOrEmpty(authorizationCode))
{
    Fail("No authorization code was returned.");
    return;
}

Info("authorization code    ", authorizationCode);


// -----------------------------------------------------------------------------------
// Step 6: Exchange the code for tokens, directly with the authorization server
// -----------------------------------------------------------------------------------
// This request does not go through the browser. It is a plain HTTPS call from this
// process to the token endpoint: the back channel. The authorization code is only
// worth something to whoever also knows the code verifier.
//
// About the client secret below: a real native application is a PUBLIC client and has
// no secret, because anything shipped inside a downloadable application can be read
// straight out of it. PKCE, not a secret, is what protects a native application. The
// secret is here only because we reuse the web demo application's confidential client,
// so that nothing has to be registered anywhere for this demo to run.

Step(6, "Exchanging the authorization code for tokens");

var tokenResponse = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(
    new Dictionary<string, string>
    {
        ["grant_type"] = "authorization_code",
        ["code"] = authorizationCode,
        ["redirect_uri"] = Settings.RedirectUri,     // Must match the one used in step 4
        ["code_verifier"] = codeVerifier,            // PKCE: the value behind the challenge
        ["client_id"] = Settings.ClientId,
        ["client_secret"] = Settings.ClientSecret
    }));

var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

if (!tokenResponse.IsSuccessStatusCode)
{
    // A refusal here is worth reading rather than hiding. Reusing a code, for example,
    // lands exactly here: authorization codes are single use.
    Fail($"The token endpoint returned {(int)tokenResponse.StatusCode} {tokenResponse.ReasonPhrase}");
    Console.WriteLine(PrettyJson(tokenJson));
    return;
}

var tokens = JsonDocument.Parse(tokenJson).RootElement;

var accessToken = tokens.GetProperty("access_token").GetString()!;
var idToken = GetOptionalString(tokens, "id_token");
var refreshToken = GetOptionalString(tokens, "refresh_token");

Console.WriteLine();
Console.WriteLine("The raw token response:");
Console.WriteLine(PrettyJson(tokenJson));

// The tokens as they arrived, and then the same tokens decoded. A JWT is not
// encrypted: anything holding one can read it. The signature is what makes it
// trustworthy, and validating that signature is the receiving API's job, not ours.
PrintToken("ID TOKEN", idToken, "Proof that the user signed in. It is what creates the local session.");
PrintToken("ACCESS TOKEN", accessToken, "Sent to APIs. It says what this client may do on the user's behalf.");
PrintToken("REFRESH TOKEN", refreshToken, "Used to get a new access token once the current one expires.");

// The nonce check. We put a random value into the authentication request, and the
// authorization server copied it into the ID token. If it does not come back
// unchanged, the ID token was not minted for this login.
if (idToken is not null && ReadJwtPayload(idToken).TryGetProperty("nonce", out var returnedNonce))
{
    Console.WriteLine();
    Info("nonce check           ", returnedNonce.GetString() == nonce
        ? "ok, the ID token belongs to this request"
        : "FAILED, the ID token does not belong to this request");
}


// -----------------------------------------------------------------------------------
// Step 7: Call the payment API with the access token
// -----------------------------------------------------------------------------------
// The access token goes into the Authorization header as a bearer token. The API
// validates it, and then decides from the scopes and claims inside whether to answer.

Step(7, "Calling the payment API with the access token");

await CallApiAsync("/identity", Settings.PaymentApiIdentityEndpoint);
await CallApiAsync("/payments", Settings.PaymentApiPaymentsEndpoint);

Banner("Done");


// -----------------------------------------------------------------------------------
//  Helpers
// -----------------------------------------------------------------------------------

// Calls a protected endpoint and prints what came back, success or refusal. The
// refusals are as interesting as the successes: sign in as the guest user and
// /payments answers 403 Forbidden, because that user has no finance role.
async Task CallApiAsync(string name, string url)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    using var response = await http.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();

    Console.WriteLine();
    Console.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
    Console.WriteLine($"GET {name} -> {(int)response.StatusCode} {response.ReasonPhrase}");
    Console.ResetColor();

    // When an API refuses a bearer token it explains itself in the WWW-Authenticate
    // header, using the error codes from RFC 6750: invalid_token, insufficient_scope
    // and so on. That header is often the fastest way to see what the API objected to.
    if (response.Headers.WwwAuthenticate.Count > 0)
        Console.WriteLine($"  WWW-Authenticate: {string.Join(", ", response.Headers.WwwAuthenticate)}");

    if (!string.IsNullOrWhiteSpace(body))
        Console.WriteLine(PrettyJson(body));
}

// Opens the user's default browser. UseShellExecute lets the operating system pick
// which browser that is, and that is the whole point: we want the browser the user is
// already signed in to, not an embedded one that this application controls.
static void OpenBrowser(string url)
{
    Process.Start(new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    });
}

// base64url: base64 without the padding, and with the two URL unsafe characters
// swapped out. Used by PKCE and by JWTs, because these values travel inside URLs.
static string Base64Url(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static byte[] Base64UrlDecode(string value)
{
    var padded = value.Replace('-', '+').Replace('_', '/');
    return Convert.FromBase64String(padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '='));
}

// Prints a token, and then its decoded header and payload when it is a JWT.
// Not every token is a JWT. A refresh token, for example, is normally an opaque
// reference that only the authorization server can make any sense of.
static void PrintToken(string title, string? token, string purpose)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"--- {title} ---");
    Console.ResetColor();
    Console.WriteLine(purpose);
    Console.WriteLine();

    if (token is null)
    {
        Console.WriteLine("(not returned)");
        return;
    }

    Console.WriteLine(token);

    var parts = token.Split('.');
    if (parts.Length != 3)
    {
        Console.WriteLine();
        Console.WriteLine("(not a JWT, so there is nothing here to decode. It is an opaque token.)");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Header:");
    Console.WriteLine(PrettyJson(Encoding.UTF8.GetString(Base64UrlDecode(parts[0]))));
    Console.WriteLine("Payload:");
    Console.WriteLine(PrettyJson(Encoding.UTF8.GetString(Base64UrlDecode(parts[1]))));
}

static JsonElement ReadJwtPayload(string jwt) =>
    JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(jwt.Split('.')[1]))).RootElement;

static string? GetOptionalString(JsonElement element, string name) =>
    element.TryGetProperty(name, out var value) ? value.GetString() : null;

static string PrettyJson(string json)
{
    try
    {
        return JsonSerializer.Serialize(
            JsonDocument.Parse(json).RootElement,
            new JsonSerializerOptions { WriteIndented = true });
    }
    catch (JsonException)
    {
        return json;   // Not JSON after all, so show it exactly as it came.
    }
}

static void Banner(string text)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"  {text}");
    Console.WriteLine(new string('=', 80));
    Console.ResetColor();
}

static void Step(int number, string text)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"[Step {number}] {text}");
    Console.ResetColor();
}

static void Info(string label, string value) => Console.WriteLine($"  {label} : {value}");

static void Fail(string message)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
}
