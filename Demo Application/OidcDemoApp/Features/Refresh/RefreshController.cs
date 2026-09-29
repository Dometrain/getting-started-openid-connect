using Duende.IdentityModel.Client;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Infrastructure.BackChannelLogger;
using OidcDemoApp.Models;
using System.IdentityModel.Tokens.Jwt;

namespace OidcDemoApp.Features.Refresh;

public class RefreshController : Controller
{
    private string RedirectUri => $"{Request.Scheme}://{Request.Host}/refresh/callback";

    public IActionResult Index()
    {
        return View(new AuthorizeRequest
        {
            ResponseType = "code",
            ClientId = Settings.RefreshClientId,
            Scope = "openid email profile api",
            Prompt = "consent",
            State = "11111111",
            Nonce = "22222222",
            ResponseMode = "form_post",
            RedirectUri = RedirectUri
        });
    }

    [HttpPost]
    public IActionResult GetAuthCode(AuthorizeRequest model)
    {
        string url = BuildCodeFlowUrl(model);

        ViewData["loginurl"] = url;

        return View();
    }

    // The authorization server posts the authorization code and state back to this
    // action (response_mode=form_post).
    [HttpPost]
    public IActionResult Callback(AuthCode authcode)
    {
        ViewData["tokenEndpoint"] = Settings.OIDCServer + "/connect/token";

        return View(authcode);
    }

    [HttpPost]
    public async Task<IActionResult> GetTokens(AuthCode authcode)
    {
        //To be secure then the state parameter should be compared to the state sent in the previous step

        var url = new Url(Settings.OIDCServer + "/connect/token");

        var token = await url.PostUrlEncodedAsync(new
        {
            client_id = Settings.RefreshClientId,
            client_secret = Settings.RefreshClientSecret,
            grant_type = "authorization_code",
            code = authcode.Code,
            redirect_uri = RedirectUri

        }).ReceiveJson<OidcTokenResponse>();

        //Save the access and refresh the token in the session cookie
        Request.HttpContext.Session.SetString("access_token", token?.AccessToken ?? "");
        Request.HttpContext.Session.SetString("refresh_token", token?.RefreshToken ?? "");
        Request.HttpContext.Session.SetString("expires_in", token?.ExpiresIn.ToString() ?? "");

        return View(token);
    }

    /// <summary>
    /// Builds the authorize URL for the authorization code flow.
    /// See the Duende IdentityServer docs for the authorize endpoint parameters:
    /// https://docs.duendesoftware.com/identityserver/v5/reference/endpoints/authorize/
    /// </summary>
    private static string BuildCodeFlowUrl(AuthorizeRequest data)
    {
        // Demo only: state and nonce are hardcoded here so they are easy to follow on
        // screen. A real client must generate a fresh random value per request.
        var url = new Url($"{Settings.OIDCServer}/connect/authorize");

        url = url.SetQueryParams(new
        {
            response_type = data?.ResponseType?.Trim(),     // "code" for the authorization code flow
            client_id = data?.ClientId?.Trim(),              // Id of this client

            scope = data?.Scope?.Trim(),                     // openid (required) plus any other scopes requested

            prompt = data?.Prompt?.Trim(),                   // Force users to provide consent

            state = data?.State?.Trim(),                     // To prevent CSRF attacks
            nonce = data?.Nonce?.Trim(),                     // To further strengthen the security

            response_mode = data?.ResponseMode?.Trim(),      // Send the token response as a form post instead of a fragment encoded redirect

            redirect_uri = data?.RedirectUri?.Trim() ?? ""   // Where the authorization server sends the browser back to after login
        });

        return url.ToString();
    }

    public IActionResult CallApi()
    {
        return View();
    }

    // Polled every second by refresh.js while the CallApi page is open.
    public async Task<IActionResult> GetData()
    {
        try
        {
            int tokenExpiresInSec = CalculateWhenAccessTokenExpires();

            var access_token = HttpContext.Session.GetString("access_token");

            if (tokenExpiresInSec < 1)
            {
                var new_access_token = await GetNewAccessToken();
                if (new_access_token != null && new_access_token.Length > 0)
                    access_token = new_access_token;
            }

            //Access the Time API using the access token
            var result = await Settings.GetTimeAPIEndpoint
                                    .WithOAuthBearerToken(access_token)
                                    .GetJsonAsync<APIResponse>();

            string str = "Data from API: " + result.Value + " (Access token expires in " + tokenExpiresInSec.ToString() + " sec)";

            return Content("{ \"name\":" + "\"" + str + "\" }");
        }
        catch (Exception exc)
        {
            return Content("{ \"name\":" + "\"" + exc.Message + "\" }");
        }
    }

    private int CalculateWhenAccessTokenExpires()
    {
        //We get the access token just to be able to calculate when it expires
        var access_token = HttpContext.Session.GetString("access_token");

        if (string.IsNullOrEmpty(access_token))
            return -99;

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(access_token);
        var ValidTo = token.ValidTo;

        var tokenExpiresInSec = (int)ValidTo.Subtract(DateTime.UtcNow).TotalSeconds;

        return tokenExpiresInSec;
    }

    /// <summary>
    /// Gets a new access token and refresh token from the authorization server using the
    /// refresh token, and updates the session cookie.
    /// </summary>
    private async Task<string?> GetNewAccessToken()
    {
        var refreshToken = HttpContext.Session.GetString("refresh_token") ?? "";

        if (string.IsNullOrEmpty(refreshToken))
            return "";

        var url = new Url($"{Settings.OIDCServer}/connect/token");

        var client = new HttpClient(new FlurlBackChannelLogger { InnerHandler = new HttpClientHandler() });
        TokenResponse response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = url,

            ClientId = Settings.RefreshClientId,
            ClientSecret = Settings.RefreshClientSecret,

            RefreshToken = refreshToken
        });

        //Save the access and refresh the token in the session cookie
        Request.HttpContext.Session.SetString("access_token", response.AccessToken ?? "");
        Request.HttpContext.Session.SetString("refresh_token", response.RefreshToken ?? "");
        Request.HttpContext.Session.SetString("expires_in", response.ExpiresIn.ToString() ?? "");

        return response.AccessToken;
    }
}

public class APIResponse
{
    public string? Value { get; set; }
}
