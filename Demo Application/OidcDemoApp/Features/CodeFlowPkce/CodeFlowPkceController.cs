using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Features.CodeFlow;
using OidcDemoApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace OidcDemoApp.Features.CodeFlowPkce;

public class CodeFlowPkceController : Controller
{
    private readonly ILogger<CodeFlowPkceController> _logger;

    private string RedirectUri => $"{Request.Scheme}://{Request.Host}/codeflowpkce/callback";

    private readonly string clientId = Settings.CodeFlowPkceClientId;

    public CodeFlowPkceController(ILogger<CodeFlowPkceController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new AuthorizeRequest
        {
            ResponseType = "code",
            ClientId = clientId,
            Scope = "openid email profile",
            Prompt = "consent",
            State = "11111111",
            Nonce = "22222222",
            ResponseMode = "form_post",
            RedirectUri = RedirectUri,
            CodeChallenge = ""
        });
    }

    public IActionResult CalculateCodeChallenge()
    {
        return View(new CodeVerifier() { Verifier = "111111111111111111111111111111111111111111111" });
    }

    [HttpPost]
    public IActionResult CalculateCodeChallenge(CodeVerifier code)
    {
        ModelState.Clear();
        if (code != null && code.Verifier != null)
        {
            code.Challenge = CalculateSha256(code.Verifier);
        }

        return View(code);
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

        try
        {
            var token = await url.PostUrlEncodedAsync(new
            {
                client_id = clientId,
                client_secret = Settings.CodeFlowPkceClientSecret,
                grant_type = "authorization_code",
                code = authcode.Code,
                redirect_uri = RedirectUri,
                code_verifier = authcode.CodeVerifier

            }).ReceiveJson<OidcTokenResponse>();

            return View(token);
        }
        catch (FlurlHttpException exception)
        {
            // A refusal from the token endpoint is one of the demos on this page, not a crash.
            // Leaving out the code_verifier is exactly what PKCE is supposed to reject, so show
            // what the OpenID Provider answered instead of the developer exception page.
            //
            // Nothing is logged here on purpose. The back-channel logger already captured this
            // call with both bodies, and the error page links straight to it.
            return View("TokenRequestFailed", await TokenRequestError.CreateAsync(exception));
        }
    }

    /// <summary>
    /// Builds the authorize URL for the authorization code flow with PKCE.
    /// See the Duende IdentityServer docs for the authorize endpoint parameters:
    /// https://docs.duendesoftware.com/identityserver/v5/reference/endpoints/authorize/
    /// </summary>
    private static string BuildCodeFlowUrl(AuthorizeRequest data)
    {
        if (data == null)
        {
            return "";
        }

        // Demo only: state and nonce are hardcoded here so they are easy to follow on
        // screen. A real client must generate a fresh random value per request.
        var url = new Url($"{Settings.OIDCServer}/connect/authorize");

        url = url.SetQueryParams(new
        {
            response_type = data.ResponseType?.Trim(),     // "code" for the authorization code flow
            client_id = data.ClientId?.Trim(),              // Id of this client

            scope = data.Scope?.Trim(),                     // openid (required) plus any other scopes requested

            prompt = data.Prompt?.Trim(),                   // Force users to provide consent

            state = data.State?.Trim(),                     // To prevent CSRF attacks
            nonce = data.Nonce?.Trim(),                      // To further strengthen the security

            response_mode = data.ResponseMode?.Trim(),      // Send the token response as a form post instead of a fragment encoded redirect

            redirect_uri = data.RedirectUri?.Trim() ?? "",   // Where the authorization server sends the browser back to after login

            code_challenge = data.CodeChallenge,             // PKCE: the hashed code verifier
            code_challenge_method = "S256",
        });

        return url.ToString();
    }

    private static string CalculateSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);

        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] arg)
    {
        string s = Convert.ToBase64String(arg); // Regular base64 encoder
        s = s.Split('=')[0]; // Remove any trailing '='s
        s = s.Replace('+', '-'); // 62nd char of encoding
        s = s.Replace('/', '_'); // 63rd char of encoding
        return s;
    }
}
