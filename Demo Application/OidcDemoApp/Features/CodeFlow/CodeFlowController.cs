using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Models;

namespace OidcDemoApp.Features.CodeFlow;

public class CodeFlowController : Controller
{
    private readonly ILogger<CodeFlowController> _logger;

    // Evaluated at access time, not at construction time
    private string RedirectUri => $"{Request.Scheme}://{Request.Host}/codeflow/callback";

    private readonly string clientId = Settings.CodeFlowClientId;

    public CodeFlowController(ILogger<CodeFlowController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new AuthorizeRequest
        {
            ResponseType = "code",
            ClientId = clientId,
            Scope = "",
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

        try
        {
            var token = await url.PostUrlEncodedAsync(new
            {
                client_id = clientId,
                client_secret = Settings.CodeFlowClientSecret,
                grant_type = "authorization_code",
                code = authcode.Code,
                redirect_uri = RedirectUri

            }).ReceiveJson<OidcTokenResponse>();

            return View(token);
        }
        catch (FlurlHttpException exception)
        {
            // A refusal from the token endpoint is a demo in its own right, not a crash. Pressing
            // the button twice reuses the code and lands here, which is worth showing: codes are
            // single use. Show what the OpenID Provider answered rather than an exception page.
            //
            // Nothing is logged here on purpose. The back-channel logger already captured this
            // call with both bodies, and the error page links straight to it.
            return View("TokenRequestFailed", await TokenRequestError.CreateAsync(exception));
        }
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
}
