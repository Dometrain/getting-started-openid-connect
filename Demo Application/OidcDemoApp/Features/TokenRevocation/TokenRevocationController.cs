using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Extensions;

namespace OidcDemoApp.Features.TokenRevocation;

public class TokenRevocationController : Controller
{
    private const string ClientIdSessionKey = "tokenrevocation:clientid";
    private const string ClientSecretSessionKey = "tokenrevocation:clientsecret";

    // The session key the refresh token demo (Features/Refresh) parks its current
    // refresh token under, so this page can offer it without a copy/paste.
    private const string RefreshTokenSessionKey = "refresh_token";

    // Prefills the client credentials of the refresh token demo and, when that demo has
    // already run in this session, the very refresh token it is currently using. Revoking
    // that token is the point of the page: the client keeps the token, keeps its session,
    // and simply fails the next time it tries to trade the token in.
    [HttpGet]
    public IActionResult Index()
    {
        var sessionToken = HttpContext.Session.GetString(RefreshTokenSessionKey);

        var model = new TokenRevocationViewModel
        {
            ClientId = HttpContext.Session.GetString(ClientIdSessionKey) ?? Settings.RefreshClientId,
            ClientSecret = HttpContext.Session.GetString(ClientSecretSessionKey) ?? Settings.RefreshClientSecret,
            Token = sessionToken,
            TokenCameFromSession = !string.IsNullOrEmpty(sessionToken),
            TokenTypeHint = "refresh_token",
        };

        return View(model);
    }

    // Calls the revocation endpoint (RFC 7009). Deliberately does NOT clear the token from
    // this application's session afterwards - the demo is that the client happily carries
    // on holding a token the authorization server has already thrown away.
    [HttpPost]
    public async Task<IActionResult> Index(TokenRevocationViewModel model)
    {
        model.StatusCode = null;
        model.RawResult = null;
        model.Error = null;

        HttpContext.Session.SetString(ClientIdSessionKey, model.ClientId ?? "");
        HttpContext.Session.SetString(ClientSecretSessionKey, model.ClientSecret ?? "");

        var url = Settings.OIDCServer + "/connect/revocation";

        var form = new Dictionary<string, string>
        {
            ["token"] = model.Token?.Trim() ?? "",
            ["client_id"] = model.ClientId ?? "",
            ["client_secret"] = model.ClientSecret ?? "",
        };

        if (!string.IsNullOrWhiteSpace(model.TokenTypeHint))
        {
            form["token_type_hint"] = model.TokenTypeHint;
        }

        // Rebuilt rather than captured, but identical to what Flurl puts on the wire.
        // The secrets in this demo are public by design, so nothing is masked here.
        model.RequestBody = string.Join("&", form.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}"));

        try
        {
            var response = await url.AllowAnyHttpStatus().PostUrlEncodedAsync(form);

            model.StatusCode = (int)response.StatusCode;

            var body = await response.GetStringAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                // A successful revocation returns 200 with nothing in it. Say so, rather
                // than rendering an empty box that looks like something went wrong.
                model.RawResult = "(empty response body)";
            }
            else
            {
                try
                {
                    model.RawResult = body.BeautifyJson();
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    model.RawResult = body;
                }
            }
        }
        catch (FlurlHttpException ex)
        {
            model.Error = ex.Message;
        }

        return View(model);
    }
}
