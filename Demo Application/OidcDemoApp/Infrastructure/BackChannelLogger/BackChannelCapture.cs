using System.Diagnostics;

namespace OidcDemoApp.Infrastructure.BackChannelLogger;

// ── Shared logging core ────────────────────────────────────────────────────

internal static class BackChannelCapture
{
    internal static async Task<HttpResponseMessage> LogAsync(
        HttpRequestMessage request,
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        var (elapsedSinceStart, elapsedSincePrevious) = BackChannelLogStore.GetTimings();

        // Read request body: HttpContent buffers on first read, safe to forward
        string? requestBody = null;
        if (request.Content is not null)
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        var callStart = Stopwatch.GetTimestamp();
        int? statusCode;
        string? responseBody = null;

        HttpResponseMessage response;
        try
        {
            response = await send();
            statusCode = (int)response.StatusCode;

            if (response.Content is not null)
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                // Re-wrap so the caller can still read the content
                var ct = response.Content.Headers.ContentType;
                var replacement = new StringContent(responseBody, System.Text.Encoding.UTF8,
                    ct?.MediaType ?? "application/json");
                if (ct?.CharSet is not null)
                    replacement.Headers.ContentType!.CharSet = ct.CharSet;
                response.Content = replacement;
            }
        }
        catch (Exception ex)
        {
            BackChannelLogStore.Add(new BackChannelLogEntry
            {
                Index = BackChannelLogStore.Count + 1,
                Timestamp = DateTime.Now,
                ElapsedSinceStart = elapsedSinceStart,
                ElapsedSincePrevious = elapsedSincePrevious,
                RequestMethod = request.Method.Method,
                RequestUrl = request.RequestUri?.AbsoluteUri ?? "",
                RequestBody = requestBody,
                ResponseStatusCode = null,
                ResponseBody = $"Exception: {ex.Message}",
                Duration = Stopwatch.GetElapsedTime(callStart),
                IsError = true
            });
            throw;
        }

        BackChannelLogStore.Add(new BackChannelLogEntry
        {
            Index = BackChannelLogStore.Count + 1,
            Timestamp = DateTime.Now,
            ElapsedSinceStart = elapsedSinceStart,
            ElapsedSincePrevious = elapsedSincePrevious,
            RequestMethod = request.Method.Method,
            RequestUrl = request.RequestUri?.AbsoluteUri ?? "",
            RequestBody = requestBody,
            ResponseStatusCode = statusCode,
            ResponseBody = responseBody,
            Duration = Stopwatch.GetElapsedTime(callStart),
            IsError = statusCode >= 400
        });

        return response;
    }
}
