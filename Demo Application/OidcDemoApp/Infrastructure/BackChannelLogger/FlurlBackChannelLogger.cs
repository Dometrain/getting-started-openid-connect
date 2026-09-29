namespace OidcDemoApp.Infrastructure.BackChannelLogger;

// ── Middleware handler used by Flurl ───────────────────────────────────────
//
//  Register once at startup:
//    FlurlHttp.Clients.WithDefaults(b => b.AddMiddleware(() => new FlurlBackChannelLogger()));
//
//  No inner handler set here: Flurl injects it automatically.

public class FlurlBackChannelLogger : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        BackChannelCapture.LogAsync(request, () => base.SendAsync(request, cancellationToken), cancellationToken);
}
