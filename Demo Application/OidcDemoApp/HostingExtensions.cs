using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using OidcDemoApp.Infrastructure.BackChannelLogger;

namespace OidcDemoApp;

public static class HostingExtensions
{

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        //Support features folder structure
        builder.Services.Configure<RazorViewEngineOptions>(rvo =>
        {
            rvo.ViewLocationFormats.Add("~/Features/{1}/{0}.cshtml");
            rvo.ViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
        });

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // Demo only: shows the full claim values (personally identifiable information)
        // in logs and exceptions, which is invaluable for teaching but must stay off
        // in a real application.
        IdentityModelEventSource.ShowPII = true;

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        });

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        }).AddCookie(opt =>
        {
            opt.LogoutPath = "/user/Logout";
            opt.AccessDeniedPath = "/user/AccessDenied";

            opt.Cookie.Name = "client-session";
            opt.Cookie.SameSite = SameSiteMode.Lax;
            opt.Cookie.HttpOnly = true;
            opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            opt.SlidingExpiration = true;

            opt.Events.OnSigningOut = async e =>
            {
                // revoke refresh token on sign-out.
                // Only an OpenID Connect login stores tokens in the cookie. The local
                // login has none, and asking to revoke when there is nothing to revoke
                // logs a confusing error, so check first.
                string? refreshToken = await e.HttpContext.GetTokenAsync("refresh_token");

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await e.HttpContext.RevokeRefreshTokenAsync();
                }
            };


        }).AddOpenIdConnect(options =>
        {
            options.Authority = Settings.OIDCServer;
            options.ClientId = Settings.LibraryLoginClientId;
            options.ClientSecret = Settings.LibraryLoginClientSecret;
            options.ResponseType = "code";

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("api");
            // Needed by the combined flows demo: the payment API's /payments endpoint
            // checks for this scope by name, so a token carrying only "api" is refused
            // even though "api" is enough to make the token's audience come out right.
            options.Scope.Add("payment");
            options.Scope.Add("offline_access");

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;
            options.AccessDeniedPath = "/User/AccessDenied";

            // Keep claim types as-is (e.g. "given_name") instead of the default
            // remapping to long legacy URIs (e.g. .../ws/2005/05/identity/claims/givenname).
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role
            };

            options.BackchannelHttpHandler = new BackChannelListener();
            options.BackchannelTimeout = TimeSpan.FromSeconds(5);

            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
            options.Prompt = "consent";
        })
        .AddOpenIdConnect(Settings.ExternalOidcScheme, "Sign-in with demo.duendesoftware.com", options =>
        {
            // Sign the user into this application's own session cookie. The snippet this
            // is based on targeted IdentityServer's external cookie
            // (IdentityServerConstants.ExternalCookieAuthenticationScheme), which only
            // exists inside an identity server. This app is a client, so the local cookie
            // is where the resulting user belongs.
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = true;

            options.Authority = Settings.ExternalOidcServer;
            options.ClientId = Settings.ExternalOidcClientId;
            options.ClientSecret = Settings.ExternalOidcClientSecret;
            options.ResponseType = "code";

            // The default /signin-oidc, /signout-callback-oidc and /signout-oidc paths are
            // already taken by the handler above, and two handlers cannot share a callback
            // path. Duende's demo clients accept any redirect URI, so these are free to pick.
            options.CallbackPath = "/signin-external-oidc";
            options.SignedOutCallbackPath = "/signout-callback-external-oidc";
            options.RemoteSignOutPath = "/signout-external-oidc";

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("email");

            options.GetClaimsFromUserInfoEndpoint = true;

            // Same reason as the handler above: without this the inbound "name" claim is
            // remapped to a legacy URI and NameClaimType below would never match it.
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role
            };

            options.BackchannelHttpHandler = new BackChannelListener();
            options.BackchannelTimeout = TimeSpan.FromSeconds(5);

            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
            options.Prompt = "consent";
        });

        builder.Services.AddOpenIdConnectAccessTokenManagement();

        // Log all Flurl HTTP calls (PostUrlEncodedAsync, etc.) to the back-channel log
        FlurlHttp.Clients.WithDefaults(b =>
            b.AddMiddleware(() => new FlurlBackChannelLogger()));

        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "Session";
        });

        // registers HTTP client that uses the managed user access token
        builder.Services.AddUserAccessTokenHttpClient("paymentapi", configureClient: client =>
        {
            client.BaseAddress = new Uri(Settings.PaymentApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSession();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");


        return app;
    }
}
