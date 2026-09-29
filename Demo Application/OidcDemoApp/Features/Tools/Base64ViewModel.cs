using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.Tools;

public class Base64ViewModel
{
    [Display(Name = "Input")]
    [DataType(DataType.MultilineText)]
    public string? Input { get; set; }

    [Display(Name = "Use Base64Url encoding")]
    public bool UseBase64Url { get; set; }

    [Display(Name = "Pretty-print the output when it is JSON")]
    public bool PrettyPrintJson { get; set; }

    public string? Output { get; set; }
    public string? Error { get; set; }
}
