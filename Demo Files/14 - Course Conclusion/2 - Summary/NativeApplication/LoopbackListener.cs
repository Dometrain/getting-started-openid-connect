using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NativeApplication;

/// <summary>
/// The one trick that makes interactive login possible from a console application.
///
/// A console application has no browser of its own, and the authorization server will
/// only ever hand the authorization code to a browser, by redirecting it. So the
/// console application starts a tiny web server on this machine, opens the system
/// browser, and waits. After the user signs in, the authorization server redirects the
/// browser to the loopback address, and that redirect is an ordinary HTTP request that
/// lands right here, inside our own process.
///
/// There is nothing clever going on between the browser and the console application.
/// The browser simply makes a request to localhost, and we happen to be listening.
///
/// We use Kestrel rather than the simpler <see cref="System.Net.HttpListener"/> for one
/// practical reason: the redirect URI registered for this demo client is HTTPS, and
/// Kestrel can serve HTTPS on localhost using the ASP.NET Core development certificate
/// that is already installed and trusted on a developer machine. HttpListener would
/// need a certificate bound to the port with netsh, which requires administrator
/// rights. For a plain HTTP loopback redirect, HttpListener would be enough.
/// </summary>
public sealed class LoopbackListener : IAsyncDisposable
{
    private readonly WebApplication _app;

    // Completed the moment the browser arrives at the callback path.
    private readonly TaskCompletionSource<IQueryCollection> _callbackReceived =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private LoopbackListener(WebApplication app) => _app = app;

    /// <summary>
    /// Starts listening on the host and path of the given redirect URI.
    /// Always start this BEFORE opening the browser, or the redirect can arrive at a
    /// port where nobody is listening.
    /// </summary>
    public static async Task<LoopbackListener> StartAsync(string redirectUri)
    {
        var uri = new Uri(redirectUri);

        var builder = WebApplication.CreateBuilder();

        // This is a console demo, so keep the console free of framework log output.
        builder.Logging.ClearProviders();

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenLocalhost(uri.Port, listenOptions =>
            {
                // No argument means "use the default certificate", which on a developer
                // machine is the ASP.NET Core development certificate created by
                // "dotnet dev-certs https --trust".
                if (uri.Scheme == Uri.UriSchemeHttps)
                    listenOptions.UseHttps();
            });
        });

        var app = builder.Build();
        var listener = new LoopbackListener(app);

        // The single endpoint this whole web server exists for.
        //
        // A GET is all we need, because we ask for response_mode=query, so the
        // authorization server redirects the browser here with the values in the URL.
        // (With form_post the browser would POST them to us in the request body instead,
        // which is what the web demo application does.)
        app.MapGet(uri.AbsolutePath, async context =>
        {
            // Hand the query string (?code=...&state=...) over to whoever is waiting.
            listener._callbackReceived.TrySetResult(context.Request.Query);

            // Give the browser something friendly to display. The user is looking at
            // the browser window right now, not at our console.
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("""
                <html>
                  <body style="font-family: sans-serif; text-align: center; margin-top: 4rem">
                    <h2>Sign in complete</h2>
                    <p>You can close this window and return to the console application.</p>
                  </body>
                </html>
                """);
        });

        await app.StartAsync();
        return listener;
    }

    /// <summary>
    /// Waits for the browser to be redirected back to us, and returns the query string
    /// parameters the authorization server sent along.
    /// </summary>
    public async Task<IQueryCollection> WaitForCallbackAsync(TimeSpan timeout)
    {
        var finished = await Task.WhenAny(_callbackReceived.Task, Task.Delay(timeout));

        if (finished != _callbackReceived.Task)
            throw new TimeoutException($"No response from the browser within {timeout.TotalMinutes:0} minutes.");

        return await _callbackReceived.Task;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
