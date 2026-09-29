using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.TokenRevocation;

public class TokenRevocationViewModel
{
    [Display(Name = "client_id")]
    public string? ClientId { get; set; }

    [Display(Name = "client_secret")]
    public string? ClientSecret { get; set; }

    [Display(Name = "token")]
    [DataType(DataType.MultilineText)]
    public string? Token { get; set; }

    [Display(Name = "token_type_hint")]
    public string? TokenTypeHint { get; set; }

    /// <summary>
    /// True when the token box was prefilled with the refresh token the refresh-token
    /// demo (Features/Refresh) is currently holding in session, so the view can say so.
    /// </summary>
    public bool TokenCameFromSession { get; set; }

    /// <summary>The form body exactly as posted, so the wire format is visible on screen.</summary>
    public string? RequestBody { get; set; }

    public int? StatusCode { get; set; }
    public string? RawResult { get; set; }
    public string? Error { get; set; }

    public bool IsSuccess => StatusCode == 200;
}
