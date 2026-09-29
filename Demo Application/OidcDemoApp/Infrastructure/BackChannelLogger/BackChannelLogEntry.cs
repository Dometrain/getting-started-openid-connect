using Newtonsoft.Json;

namespace OidcDemoApp.Infrastructure.BackChannelLogger;

// ── Immutable log entry ────────────────────────────────────────────────────

public class BackChannelLogEntry
{
    public int Index { get; init; }
    public DateTime Timestamp { get; init; }
    public TimeSpan ElapsedSinceStart { get; init; }
    public TimeSpan? ElapsedSincePrevious { get; init; }
    public string RequestMethod { get; init; } = "";
    public string RequestUrl { get; init; } = "";
    public string? RequestBody { get; init; }
    public int? ResponseStatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public TimeSpan Duration { get; init; }
    public bool IsError { get; init; }

    public string PrettyRequestBody => TryPrettyJson(RequestBody);
    public string PrettyResponseBody => TryPrettyJson(ResponseBody);

    private static string TryPrettyJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? "";
        try
        {
            var obj = JsonConvert.DeserializeObject(value);
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        catch
        {
            return value;
        }
    }
}
