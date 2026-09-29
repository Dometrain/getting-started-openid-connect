using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace OidcDemoApp.Features.UrlTools;

public class UrlToolsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new UrlParseViewModel());
    }

    [HttpPost]
    public IActionResult Index(UrlParseViewModel model)
    {
        model.Rows.Clear();
        model.Error = null;
        model.BaseUrl = null;
        model.FragmentPath = null;

        if (!ModelState.IsValid)
            return View(model);

        var raw = (model.InputUrl ?? "").Trim();

        // Allow users to paste multi-line URLs (like in slides)
        raw = raw.Replace("\r", "").Replace("\n", "");

        if (raw.Contains('#') && !raw.Contains('?'))
        {
            // If the URL contains # but no ?, then replace # with a ?
            raw = raw.Replace('#', '?');
        }


        if (!TryParseUrl(raw, out var uri, out var error))
        {
            model.Error = error;
            return View(model);
        }

        // Base URL (scheme + host + path)
        model.BaseUrl = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : ":" + uri.Port)}{uri.AbsolutePath}";

        // 1) Parse normal query string (?a=b&c=d)
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var q = QueryHelpers.ParseQuery(uri.Query);
            AddRows(model, q, fromFragment: false);
        }

        // 2) Parse fragment query (#?a=b&c=d)
        // Fragment can be like "#?client_id=...&scope=..." or "#/something?x=y"
        var frag = uri.Fragment; // includes leading '#'
        if (!string.IsNullOrEmpty(frag))
        {
            var fragNoHash = frag.Substring(1);
            model.FragmentPath = fragNoHash;

            // Find a '?' inside the fragment
            var idx = fragNoHash.IndexOf('?');
            if (idx >= 0 && idx < fragNoHash.Length - 1)
            {
                var fragQuery = fragNoHash.Substring(idx); // includes '?'
                var fq = QueryHelpers.ParseQuery(fragQuery);
                AddRows(model, fq, fromFragment: true);
            }
        }

        // Sort rows by key for readability (optional)
        model.Rows = model.Rows
            .OrderBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.FromFragment ? 1 : 0)
            .ToList();

        return View(model);
    }

    private static void AddRows(UrlParseViewModel model,
                                IDictionary<string, StringValues> parsed,
                                bool fromFragment)
    {
        foreach (var kvp in parsed)
        {
            var key = kvp.Key;

            // Some parameters can appear multiple times. Show each value on its own line.
            foreach (var v in kvp.Value)
            {
                model.Rows.Add(new QueryRow
                {
                    Key = key,
                    Value = v ?? "",
                    FromFragment = fromFragment
                });
            }
        }
    }

    private static bool TryParseUrl(string raw, out Uri uri, out string? error)
    {
        error = null;

        // If user pastes without scheme, try adding https://
        if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            raw = "https://" + raw;
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out uri!))
        {
            error = "That doesn't look like a valid URL.";
            return false;
        }

        return true;
    }
}
