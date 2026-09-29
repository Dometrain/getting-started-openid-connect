using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.Tools;

public class HashViewModel
{
    [Display(Name = "Text to hash")]
    [DataType(DataType.MultilineText)]
    public string? Input { get; set; }

    [Display(Name = "Output format")]
    public HashOutputFormat Format { get; set; } = HashOutputFormat.Hex;

    public List<HashResult> Results { get; } = [];
}

public enum HashOutputFormat
{
    Hex,
    Base64,
    Base64Url,
}

public record HashResult(string Algorithm, int Bits, string Value);
