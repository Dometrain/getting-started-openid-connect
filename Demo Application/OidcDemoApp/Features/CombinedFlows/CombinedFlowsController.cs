using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Extensions;
using OidcDemoApp.Models;
using System.IdentityModel.Tokens.Jwt;

namespace OidcDemoApp.Features.CombinedFlows;

/// <summary>
/// Shows one client holding two access tokens at the same time and using each for the
/// job it is meant for: its own token for background work that has nothing to do with a
/// user, and the signed-in user's token for work done on that user's behalf.
///
/// Two steps on purpose. Step one asks for the client's own token, so the request is a
/// visible action rather than something the page did on its own. Step two calls the API
/// with whatever tokens are on the page.
///
/// The client-credentials half runs whether or not anybody is signed in. That is the
/// point: background work does not stop when the user goes away.
/// </summary>
public class CombinedFlowsController : Controller
{
    // Nothing but the token request form to begin with. The user's token is not looked
    // up yet - it appears alongside the client's token once the first button has run,
    // so both tokens land on screen at the same moment.
    [HttpGet]
    public IActionResult Index()
    {
        return View(new CombinedFlowsViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string? callapis, CombinedFlowsViewModel model)
    {
        // The user's token is never posted back - it is read from the session cookie on
        // every request, because that is where the sign-in put it.
        await AddUserTokenAsync(model);

        if (callapis == null)
        {
            // Step one: ask the authorization server for a token in this application's
            // own name. No browser, no user, no redirect - a back-channel call with the
            // client's credentials.
            await RequestClientTokenAsync(model);
        }
        else
        {
            // Step two: use the token from step one, exactly as shown on the page.
            DescribeClientToken(model);

            model.WasCalled = true;

            // Service work. The client acts as itself, so this always runs.
            model.ServiceCall = await CallApiAsync(
                Settings.PaymentApiIdentityEndpoint,
                model.ClientAccessToken,
                "Client access token (client credentials)");

            // User work. Only possible when a user is present to have delegated it.
            if (model.IsUserSignedIn)
            {
                model.UserCall = await CallApiAsync(
                    Settings.PaymentApiPaymentsEndpoint,
                    model.UserAccessToken,
                    "User access token (authorization code)");
            }
        }

        // Values set above are on the model, not in ModelState, and the tag helpers read
        // ModelState first. Without this the token box would render the posted value.
        ModelState.Clear();

        return View(nameof(Index), model);
    }

    private static async Task RequestClientTokenAsync(CombinedFlowsViewModel model)
    {
        try
        {
            var token = await (Settings.OIDCServer + "/connect/token")
                .PostUrlEncodedAsync(new
                {
                    grant_type = model.GrantType,
                    client_id = model.ClientId,
                    client_secret = model.ClientSecret,
                    scope = model.Scope,
                })
                .ReceiveJson<OidcTokenResponse>();

            model.ClientAccessToken = token.AccessToken;
            DescribeClientToken(model);
        }
        catch (Exception ex)
        {
            model.ClientTokenError = ex.Message;
        }
    }

    private static void DescribeClientToken(CombinedFlowsViewModel model)
    {
        model.ClientScopes = JoinClaims(model.ClientAccessToken, "scope", " ");
    }

    // Reads the access token the OpenID Connect handler parked in the session cookie at
    // login. Roles and scopes come from that token rather than from the cookie's own
    // claims, because the payment API only ever sees the token - and role arrives as an
    // ApiResource user claim, so it is in the access token but not in the id_token.
    private async Task AddUserTokenAsync(CombinedFlowsViewModel model)
    {
        model.IsUserSignedIn = User.Identity?.IsAuthenticated == true;

        if (!model.IsUserSignedIn)
        {
            return;
        }

        model.UserDisplayName = User.Identity?.Name;
        model.UserSubject = User.FindFirst("sub")?.Value;
        model.UserAccessToken = await HttpContext.GetTokenAsync("access_token");

        model.UserRoles = JoinClaims(model.UserAccessToken, "role", ", ");
        model.UserScopes = JoinClaims(model.UserAccessToken, "scope", " ");
    }

    private static async Task<ApiCallResult> CallApiAsync(string url, string? accessToken, string tokenDescription)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new ApiCallResult
            {
                Url = url,
                TokenDescription = tokenDescription,
                Error = "No access token available, so the call was not made.",
            };
        }

        try
        {
            var response = await url
                .AllowAnyHttpStatus()
                .WithOAuthBearerToken(accessToken)
                .GetAsync();

            var body = await response.GetStringAsync();

            return new ApiCallResult
            {
                Url = url,
                TokenDescription = tokenDescription,
                StatusCode = response.StatusCode,
                Body = Prettify(body),
            };
        }
        catch (Exception ex)
        {
            return new ApiCallResult
            {
                Url = url,
                TokenDescription = tokenDescription,
                Error = ex.Message,
            };
        }
    }

    private static string? JoinClaims(string? jwt, string claimType, string separator)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        try
        {
            var values = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt).Claims
                .Where(c => c.Type == claimType)
                .Select(c => c.Value);

            var joined = string.Join(separator, values);

            return string.IsNullOrEmpty(joined) ? null : joined;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Prettify(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "(empty response body)";
        }

        try
        {
            return body.BeautifyJson();
        }
        catch (Newtonsoft.Json.JsonException)
        {
            return body;
        }
    }
}
