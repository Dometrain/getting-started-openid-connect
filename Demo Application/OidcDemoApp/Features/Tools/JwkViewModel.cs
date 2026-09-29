using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.Tools;

public class JwkViewModel
{
    [Display(Name = "Key (PEM)")]
    [DataType(DataType.MultilineText)]
    public string Pem { get; set; } = "";

    // For example "RSA, 2048-bit" or "EC, P-256".
    public string? Description { get; set; }

    public string? Jwk { get; set; }
    public string? Kid { get; set; }

    // The private form, with the private members added. Produced whenever a private key was
    // pasted, and marked on screen as the one that must never be published.
    public string? PrivateJwk { get; set; }

    public string? Error { get; set; }
}
