using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OidcDemoApp.Extensions;
using System.Text;

namespace OidcDemoApp.Features.Tokens;

public class TokensController : Controller
{
    private const string IntrospectClientIdSessionKey = "tokenintrospection:clientid";
    private const string IntrospectClientSecretSessionKey = "tokenintrospection:clientsecret";

    public IActionResult Index()
    {
        return View(new TokenModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(TokenModel model)
    {
        model.CalledUrl = Settings.OIDCServer + "/connect/userinfo";
        model.Result = await CallEndpointAsync(model.CalledUrl, model.AccessToken);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GetTime(TokenModel model)
    {
        model.CalledUrl = Settings.GetTimeAPIEndpoint;
        model.Result = await CallEndpointAsync(model.CalledUrl, model.AccessToken);

        return View("Index", model);
    }

    // The three payment API endpoints below all sit behind the same bearer-token
    // authentication but demand different things from the token, so the same pasted
    // token can succeed on one and be rejected by the next. That contrast is the demo.

    // Any authenticated caller. Echoes back every claim on the token.
    [HttpPost]
    public async Task<IActionResult> PaymentApiIdentity(TokenModel model)
    {
        model.CalledUrl = Settings.PaymentApiIdentityEndpoint;
        model.Result = await CallEndpointAsync(model.CalledUrl, model.AccessToken);

        return View("Index", model);
    }

    // Requires scope=payment and role=finance.
    [HttpPost]
    public async Task<IActionResult> PaymentApiPayments(TokenModel model)
    {
        model.CalledUrl = Settings.PaymentApiPaymentsEndpoint;
        model.Result = await CallEndpointAsync(model.CalledUrl, model.AccessToken);

        return View("Index", model);
    }

    // Requires scope=payment and role=developer.
    [HttpPost]
    public async Task<IActionResult> PaymentApiNewFeature(TokenModel model)
    {
        model.CalledUrl = Settings.PaymentApiNewFeatureEndpoint;
        model.Result = await CallEndpointAsync(model.CalledUrl, model.AccessToken);

        return View("Index", model);
    }

    [HttpGet]
    public IActionResult Introspect()
    {
        var model = new IntrospectViewModel
        {
            ClientId = HttpContext.Session.GetString(IntrospectClientIdSessionKey),
            ClientSecret = HttpContext.Session.GetString(IntrospectClientSecretSessionKey),
        };

        return View(model);
    }

    // Calls the introspection endpoint (RFC 7662) with client credentials plus the token
    // to inspect, and pulls "active"/"exp" out of the response so the view can highlight
    // them, regardless of whether the rest of the parsing succeeds. When ReturnAsJwt is
    // set, requests the RFC 9701 JWT response format instead of plain JSON.
    [HttpPost]
    public async Task<IActionResult> Introspect(IntrospectViewModel model)
    {
        model.StatusCode = null;
        model.RawResult = null;
        model.DecodedResult = null;
        model.Error = null;
        model.IsActive = null;
        model.IsExpired = null;
        model.ExpiryDisplay = null;

        HttpContext.Session.SetString(IntrospectClientIdSessionKey, model.ClientId ?? "");
        HttpContext.Session.SetString(IntrospectClientSecretSessionKey, model.ClientSecret ?? "");

        var url = Settings.OIDCServer + "/connect/introspect";

        try
        {
            var request = url.AllowAnyHttpStatus();

            if (model.ReturnAsJwt)
            {
                request = request.WithHeader("Accept", "application/token-introspection+jwt");
            }

            var response = await request.PostUrlEncodedAsync(new
            {
                token = model.Token,
                client_id = model.ClientId,
                client_secret = model.ClientSecret,
            });

            model.StatusCode = (int)response.StatusCode;

            var body = await response.GetStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                if (model.ReturnAsJwt)
                {
                    ParseJwtIntrospectionResponse(model, body);
                }
                else
                {
                    ParseJsonIntrospectionResponse(model, body);
                }
            }
        }
        catch (FlurlHttpException ex)
        {
            model.Error = ex.Message;
        }

        return View(model);
    }

    private static void ParseJsonIntrospectionResponse(IntrospectViewModel model, string body)
    {
        try
        {
            model.RawResult = body.BeautifyJson();

            var json = JObject.Parse(body);
            model.IsActive = json["active"]?.Value<bool?>();

            // Per RFC 7662, servers commonly return just {"active": false} with no
            // other claims when the token is inactive, so exp will be missing here.
            // Fall back to decoding the exp claim from the submitted JWT itself.
            var exp = json["exp"]?.Value<long?>() ?? TryGetJwtExpiry(model.Token);
            ApplyExpiry(model, exp);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            model.RawResult = body;
        }
    }

    // The response body itself is a JWT (header.payload.signature). The actual
    // introspection claims live nested under a "token_introspection" claim in its payload.
    private static void ParseJwtIntrospectionResponse(IntrospectViewModel model, string body)
    {
        model.RawResult = body;

        var payloadJson = TryDecodeJwtPayload(body);
        if (payloadJson == null)
            return;

        try
        {
            var payload = JObject.Parse(payloadJson);

            if (payload["token_introspection"] is JObject introspection)
            {
                model.DecodedResult = introspection.ToString(Newtonsoft.Json.Formatting.Indented);
                model.IsActive = introspection["active"]?.Value<bool?>();

                var exp = introspection["exp"]?.Value<long?>() ?? TryGetJwtExpiry(model.Token);
                ApplyExpiry(model, exp);
            }
        }
        catch (Newtonsoft.Json.JsonException)
        {
            // Leave DecodedResult unset; the raw JWT is still shown.
        }
    }

    private static void ApplyExpiry(IntrospectViewModel model, long? exp)
    {
        if (!exp.HasValue)
            return;

        var remaining = DateTimeOffset.FromUnixTimeSeconds(exp.Value) - DateTimeOffset.UtcNow;
        model.IsExpired = remaining < TimeSpan.Zero;
        model.ExpiryDisplay = FormatExpiry(remaining);
    }

    // Reads the exp claim straight out of a JWT's payload segment, bypassing the
    // introspection response entirely. Returns null for opaque/reference tokens.
    private static long? TryGetJwtExpiry(string? token)
    {
        var payloadJson = TryDecodeJwtPayload(token);
        if (payloadJson == null)
            return null;

        try
        {
            return JObject.Parse(payloadJson)["exp"]?.Value<long?>();
        }
        catch (Newtonsoft.Json.JsonException)
        {
            return null;
        }
    }

    private static string? TryDecodeJwtPayload(string? jwt)
    {
        var parts = jwt?.Split('.');
        if (parts is not { Length: 3 })
            return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            var padding = payload.Length % 4;
            if (padding > 0)
                payload += new string('=', 4 - padding);

            return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string FormatExpiry(TimeSpan remaining)
    {
        var expired = remaining < TimeSpan.Zero;
        var abs = remaining.Duration();
        var formatted = $"{(int)abs.TotalMinutes}:{abs.Seconds:D2}";
        return expired ? $"Expired {formatted} ago" : $"Expires in {formatted}";
    }

    // Calls the endpoint with the pasted access token and formats the raw HTTP response
    // (status line, headers, body) exactly as returned, success or failure, so students
    // see what the API actually sent back rather than a friendly error message.
    private static async Task<string> CallEndpointAsync(string url, string? accessToken)
    {
        var request = url.AllowAnyHttpStatus();

        // With no token pasted, send no Authorization header at all rather than an
        // empty "Bearer" one. An empty header is not something a real client would
        // send, and it confuses what the API's response is actually reacting to.
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request = request.WithOAuthBearerToken(accessToken);
        }

        var response = await request.GetAsync();

        var body = await response.GetStringAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"HTTP {(int)response.StatusCode} {response.ResponseMessage.ReasonPhrase}");

        foreach (var header in response.Headers)
        {
            sb.AppendLine($"{header.Name}: {header.Value}");
        }

        sb.AppendLine();

        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                sb.Append(body.BeautifyJson());
            }
            catch (Newtonsoft.Json.JsonException)
            {
                sb.Append(body);
            }
        }

        return sb.ToString();
    }
}
