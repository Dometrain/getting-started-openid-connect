using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OidcDemoApp.Features.Tools;

public class SymmetricTokenViewModel : TokenBuilderViewModel
{
    [Display(Name = "Shared secret")]
    public string Secret { get; set; } = "";

    public int SecretBits => Encoding.UTF8.GetByteCount(Secret ?? "") * 8;

    // RFC 7518 section 3.2 requires an HS256 key to be at least as long as the SHA-256
    // value it produces. Microsoft.IdentityModel enforces exactly that: 31 bytes throws,
    // 32 bytes validates. Raw HMAC has no such limit, which is why this page can still
    // sign with a short key, and why the page is a good place to show the difference.
    public bool SecretTooShort => SecretBits < 256;
}
