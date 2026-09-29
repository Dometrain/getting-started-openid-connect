using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.Tokens;

public class IntrospectViewModel
{
    [Display(Name = "client_id")]
    public string? ClientId { get; set; }

    [Display(Name = "client_secret")]
    public string? ClientSecret { get; set; }

    [Display(Name = "token")]
    [DataType(DataType.MultilineText)]
    public string? Token { get; set; }

    [Display(Name = "Return as JWT")]
    public bool ReturnAsJwt { get; set; }

    public int? StatusCode { get; set; }
    public string? RawResult { get; set; }
    public string? DecodedResult { get; set; }
    public string? Error { get; set; }

    public bool? IsActive { get; set; }
    public bool? IsExpired { get; set; }
    public string? ExpiryDisplay { get; set; }
}
