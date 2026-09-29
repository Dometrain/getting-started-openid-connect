namespace OidcDemoApp.Infrastructure.BackChannelLogger;

// ── Handler used by AddOpenIdConnect BackchannelHttpHandler ───────────────

public class BackChannelListener : DelegatingHandler
{
    public BackChannelListener() : base(new HttpClientHandler()) { }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        BackChannelCapture.LogAsync(request, () => base.SendAsync(request, cancellationToken), cancellationToken);
}
