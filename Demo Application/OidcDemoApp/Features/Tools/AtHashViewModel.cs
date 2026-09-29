using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.Tools;

public class AtHashViewModel
{
    [Display(Name = "Access token")]
    [DataType(DataType.MultilineText)]
    public string? AccessToken { get; set; }

    public AtHashResult? Result { get; set; }

    public string? Error { get; set; }
}

// Every stage of the calculation, not just the answer. The two halves of the digest are kept
// apart because the truncation is the step this page exists to show.
public record AtHashResult(
    string HashedToken,
    bool WhitespaceRemoved,
    string KeptHex,
    string DiscardedHex,
    int DigestBytes,
    int KeptBytes,
    string AtHash);
