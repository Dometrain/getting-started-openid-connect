using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OidcDemoApp.Models;

// What the token endpoint said when it refused the request.
//
// A 400 from the token endpoint is a normal thing to demonstrate on camera, not a crash:
// leaving out the code_verifier, reusing an authorization code, or letting one expire all land
// there. Letting the Flurl exception reach the developer exception page hides the interesting
// part, which is the response body. RFC 6749 section 5.2 defines what that body contains.
public class TokenRequestError
{
    public string Endpoint { get; init; } = "";

    public int? StatusCode { get; init; }

    // The "error" code from the response, for example "invalid_grant". Null if the response was
    // not the JSON error object the specification asks for.
    public string? Error { get; init; }

    // Optional per RFC 6749, and authorization servers routinely leave it out on purpose: a
    // precise explanation of why a grant was rejected would also help an attacker.
    public string? ErrorDescription { get; init; }

    public string? RawBody { get; init; }

    public static async Task<TokenRequestError> CreateAsync(FlurlHttpException exception)
    {
        var body = await exception.GetResponseStringAsync();

        string? error = null;
        string? errorDescription = null;

        try
        {
            var json = JObject.Parse(body ?? "");

            error = (string?)json["error"];
            errorDescription = (string?)json["error_description"];
        }
        catch (JsonException)
        {
            // Not the JSON error object the specification asks for. The raw body is still shown,
            // which is the honest thing to do when a server answers with something else.
        }

        return new TokenRequestError
        {
            Endpoint = exception.Call?.Request?.Url?.ToString() ?? "",
            StatusCode = exception.StatusCode,
            Error = error,
            ErrorDescription = errorDescription,
            RawBody = string.IsNullOrWhiteSpace(body) ? null : body.Trim(),
        };
    }
}
