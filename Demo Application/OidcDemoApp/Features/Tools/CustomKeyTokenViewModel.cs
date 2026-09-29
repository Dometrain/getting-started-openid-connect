using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.Tools;

// The three JWT signature families that sign with a private key, named as the slides name
// them. The hash size is not chosen here: signing produces all three at once so they can be
// compared.
public enum SignatureFamily
{
    RS,
    PS,
    ES,
}

// One row of the result: either a signed token, or the reason this hash size is not
// available for the key that was pasted.
public sealed record SignedVariant(
    string Algorithm,
    string? EncodedHeader,
    string? Signature,
    int? SignatureBytes,
    string? Token,
    string? Unavailable);

public class CustomKeyTokenViewModel : TokenBuilderViewModel
{
    [Display(Name = "Private key (PEM)")]
    [DataType(DataType.MultilineText)]
    public string PrivateKey { get; set; } = "";

    public SignatureFamily Family { get; set; } = SignatureFamily.RS;

    // What the pasted key turned out to be, for example "RSA, 2048-bit" or "EC, P-256".
    // Shown next to the signatures, because the key is what decides their length.
    public string? KeyDescription { get; set; }

    // Why the key could not be used at all: wrong type for the chosen family, encrypted,
    // or not a key. Per-hash-size problems live on the variant instead.
    public string? KeyError { get; set; }

    // One entry per hash size, always three, in 256/384/512 order.
    public List<SignedVariant> Variants { get; } = [];

    public static readonly (SignatureFamily Family, string Scheme, string Summary)[] FamilyChoices =
    [
        (SignatureFamily.RS, "RSASSA-PKCS1-v1_5", "The original RSA scheme. Deterministic: the same input signed twice gives the same signature."),
        (SignatureFamily.PS, "RSASSA-PSS", "The newer RSA scheme. Adds randomness while signing, so the same input gives a different signature every time."),
        (SignatureFamily.ES, "ECDSA", "Elliptic curve. Much smaller keys and signatures, and the curve decides which hash size you may use."),
    ];
}
