using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.UrlTools;

public class UrlParseViewModel
{
    [Display(Name = "Authorization URL")]
    [Required(ErrorMessage = "Please paste a URL.")]
    [DataType(DataType.MultilineText)]
    public string? InputUrl { get; set; }

    // Output
    public string? BaseUrl { get; set; }
    public string? FragmentPath { get; set; } // e.g. oauth20_authorize.srf#?...
    public string? Error { get; set; }

    public List<QueryRow> Rows { get; set; } = [];
}

public class QueryRow
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public bool FromFragment { get; set; } // true if parsed from "#?..."
}
