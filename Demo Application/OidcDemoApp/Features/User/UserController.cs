using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace OidcDemoApp.Features.User;

public class UserController : Controller
{
    //The authentication type stamped on the identity created by the local login.
    //"pwd" is the standard OpenID Connect value for "authenticated with a password".
    private const string LocalAuthenticationType = "pwd";

    [HttpPost]
    public async Task Login()
    {
        await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties()
            {
                RedirectUri = "/"
            });
    }

    /// <summary>
    /// Logs in against Duende's public demo provider instead of the course's own one,
    /// to show the exact same code flow running against a completely different OpenID
    /// Provider. Note the different issuer, claims, and consent screen that come back.
    /// </summary>
    [HttpPost]
    public async Task ExternalLogin()
    {
        await HttpContext.ChallengeAsync(Settings.ExternalOidcScheme,
            new AuthenticationProperties()
            {
                RedirectUri = "/"
            });
    }

    /// <summary>
    /// A local login that never talks to the OpenID Provider. It builds a hardcoded
    /// user in code and signs it straight into the local cookie, so the demos can show
    /// what a session cookie, its claims, and its authentication properties look like
    /// on their own, with no protocol traffic in the way.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> LocalLogin()
    {
        const string userName = "Bob Smith";

        var myClaims = new List<Claim>()
        {
            new Claim("sub","12345"), //sub = subject = UserId
            new Claim("name", userName),
            new Claim("email", "bob@tn-data.se"),
            new Claim("role", "developer"),
            new Claim("role", "admin"),
            new Claim("role", "sales"),
        };

        var myIdentity = new ClaimsIdentity(claims: myClaims,
                                            authenticationType: LocalAuthenticationType,
                                            nameType: "name",
                                            roleType: "role");

        var myPrincipal = new ClaimsPrincipal(myIdentity);

        await HttpContext.SignInAsync(myPrincipal);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Clears the local session cookie only. The session at the OpenID Provider is left
    /// untouched, so the next OpenID Connect login signs the user straight back in
    /// without asking for credentials. That is single sign-on, and seeing it happen is
    /// the point of having this as its own menu option.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> LocalLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Clears the local session cookie and then signs out at the OpenID Provider through
    /// its end-session endpoint, ending the single sign-on session as well.
    /// </summary>
    [HttpPost]
    public async Task Logout()
    {
        // Sign out at whichever provider actually signed this user in. The remote
        // handler records its own scheme name under ".AuthScheme" when it creates the
        // cookie, so an external login ends its session at the external provider
        // rather than at the course's own one.
        var authenticateResult = await HttpContext.AuthenticateAsync();
        string signOutScheme = OpenIdConnectDefaults.AuthenticationScheme;

        if (authenticateResult.Properties?.Items.TryGetValue(".AuthScheme", out var usedScheme) == true
            && !string.IsNullOrEmpty(usedScheme))
        {
            signOutScheme = usedScheme;
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(signOutScheme);

        //Important, this method should never return anything.
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Info()
    {
        string idToken = await HttpContext.GetTokenAsync("id_token") ?? "";
        string accessToken = await HttpContext.GetTokenAsync("access_token") ?? "";
        string refreshToken = await HttpContext.GetTokenAsync("refresh_token") ?? "";

        //To prevent XSS, make sure the token only contains valid base64 characters or ".-"
        //https://en.wikipedia.org/wiki/Base64#Variants_summary_table
        var regex = new Regex(@"^[\w\+\/\=\.-]+$");  //Matches a-z, A-Z, 0-9, including the _ (underscore) character.

        if (regex.IsMatch(idToken))
        {
            ViewData["idToken"] = idToken;
        }
        if (regex.IsMatch(accessToken))
        {
            ViewData["accessToken"] = accessToken;
        }
        if (regex.IsMatch(refreshToken))
        {
            ViewData["refreshToken"] = refreshToken;
        }

        return View();
    }
}
