using Flurl;
using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Models;

namespace OidcDemoApp.Features.ImplicitFlow;

public class ImplicitFlowController : Controller
{
    // Evaluated at access time, not at construction time
    private string RedirectUriFragment => $"{Request.Scheme}://{Request.Host}/ImplicitFlow/LoggedInUsingFragment";
    private string RedirectUriPostback => $"{Request.Scheme}://{Request.Host}/ImplicitFlow/LoggedInUsingPostBack";

    // Start page for the login using the fragment response mode
    public IActionResult Index()
    {
        return View(new AuthorizeRequest
        {
            ClientId = Settings.ImplicitFlowClientId,
            Prompt = "consent",
            RedirectUri = RedirectUriFragment
        });
    }

    // Start page for the login using the form_post response mode
    public IActionResult Postback()
    {
        return View(new AuthorizeRequest
        {
            ClientId = Settings.ImplicitFlowClientId,
            Prompt = "consent",
            RedirectUri = RedirectUriPostback
        });
    }

    // Shortcut for the live demo: skips the manual "create the login link" form and sends
    // the browser straight to the authorization server with the parameters filled in.
    public IActionResult DirectFragment()
    {
        string url = BuildFragmentUrl(new AuthorizeRequest
        {
            ResponseType = "id_token token",
            ClientId = Settings.ImplicitFlowClientId,
            Scope = "openid email profile",
            Prompt = "login",
            State = "1111",
            Nonce = "2222",
            RedirectUri = RedirectUriFragment
        });

        return Redirect(url);
    }

    [HttpPost]
    public IActionResult LoginUsingFragment(AuthorizeRequest data)
    {
        string url = BuildFragmentUrl(data);

        ViewData["loginurl"] = url;
        ViewData["state"] = data?.State ?? "";
        ViewData["nonce"] = data?.Nonce ?? "";

        return View();
    }

    // The authorization server posts the tokens back to this action when
    // response_mode=form_post is used.
    [HttpPost]
    public IActionResult LoggedInUsingPostBack(string id_token, string access_token, string token_type, string expires_in, string scope, string state, string session_state)
    {
        ViewData["scope"] = scope;
        ViewData["expires_in"] = expires_in;
        ViewData["token_type"] = token_type;
        ViewData["state"] = state;
        ViewData["id_token"] = id_token;
        ViewData["access_token"] = access_token;
        ViewData["session_state"] = session_state;

        return View();
    }

    // The authorization server redirects back to this page with the tokens in the
    // URL fragment when response_mode=fragment is used.
    public IActionResult LoggedInUsingFragment()
    {
        return View();
    }

    /// <summary>
    /// Builds the authorize URL for the implicit flow.
    /// See the Duende IdentityServer docs for the authorize endpoint parameters:
    /// https://docs.duendesoftware.com/identityserver/v5/reference/endpoints/authorize/
    /// </summary>
    private static string BuildFragmentUrl(AuthorizeRequest data)
    {
        var url = new Url($"{Settings.OIDCServer}/connect/authorize");

        url = url.SetQueryParams(new
        {
            response_type = data?.ResponseType?.Trim(),     // "id_token token" or "id_token"
            client_id = data?.ClientId?.Trim(),              // Id of this client

            scope = data?.Scope?.Trim(),                     // openid (required) plus any other scopes requested

            prompt = data?.Prompt?.Trim(),                   // Force users to provide consent

            state = data?.State?.Trim(),                     // To prevent CSRF attacks
            nonce = data?.Nonce?.Trim(),                     // To further strengthen the security

            response_mode = data?.ResponseMode?.Trim(),      // "fragment" (default) or "form_post"

            redirect_uri = data?.RedirectUri?.Trim() ?? ""   // Where the authorization server sends the browser back to after login
        });

        return url.ToString();
    }
}
