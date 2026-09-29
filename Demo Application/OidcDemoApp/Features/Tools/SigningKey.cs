using System.Security.Cryptography;

namespace OidcDemoApp.Features.Tools;

// Wraps whatever private key was pasted into the custom key token generator. Kept separate
// from the controller because loading the key is where most of the teaching happens: the
// key decides which algorithms are even possible, and says so in its own error messages.
public sealed class SigningKey : IDisposable
{
    private readonly RSA? _rsa;
    private readonly ECDsa? _ecdsa;
    private readonly bool _usePss;

    private SigningKey(RSA? rsa, ECDsa? ecdsa, bool usePss, string description)
    {
        _rsa = rsa;
        _ecdsa = ecdsa;
        _usePss = usePss;
        Description = description;
    }

    // For example "RSA, 2048-bit" or "EC, P-256". Shown beside the signatures, because the
    // key is what decides how long they are.
    public string Description { get; }

    public static SigningKey? TryLoad(string? pem, SignatureFamily family, out string? error)
    {
        error = null;

        var text = (pem ?? "").Trim();

        if (text.Length == 0)
        {
            error = "Paste a private key in PEM format. Generate one with OpenSSL first.";
            return null;
        }

        // A passphrase-protected key needs the passphrase to import, and asking for one on
        // this page would add a field that teaches nothing. Say so plainly instead of
        // letting the import throw something cryptic.
        if (text.Contains("ENCRYPTED", StringComparison.Ordinal))
        {
            error = "This key is passphrase protected. Generate an unencrypted demo key, or decrypt this one with OpenSSL first.";
            return null;
        }

        return family == SignatureFamily.ES
            ? TryLoadEllipticCurve(text, out error)
            : TryLoadRsa(text, family == SignatureFamily.PS, out error);
    }

    private static SigningKey? TryLoadRsa(string pem, bool usePss, out string? error)
    {
        error = null;

        var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (ArgumentException)
        {
            rsa.Dispose();
            error = DescribeWrongKey(pem, "RS and PS");
            return null;
        }
        catch (CryptographicException)
        {
            rsa.Dispose();
            error = DescribeWrongKey(pem, "RS and PS");
            return null;
        }

        return new SigningKey(rsa, null, usePss, $"RSA, {rsa.KeySize}-bit");
    }

    private static SigningKey? TryLoadEllipticCurve(string pem, out string? error)
    {
        error = null;

        var ecdsa = ECDsa.Create();

        try
        {
            ecdsa.ImportFromPem(pem);
        }
        catch (ArgumentException)
        {
            ecdsa.Dispose();
            error = DescribeWrongKey(pem, "ES");
            return null;
        }
        catch (CryptographicException)
        {
            ecdsa.Dispose();
            error = DescribeWrongKey(pem, "ES");
            return null;
        }

        return new SigningKey(null, ecdsa, false, $"EC, {CurveName(ecdsa.KeySize)}");
    }

    private static string DescribeWrongKey(string pem, string family)
    {
        // The most common mistake here is pasting the right key into the wrong family, so
        // check for that before falling back to "this is not a key at all".
        if (family == "ES" && LooksLikeRsa(pem))
        {
            return "This is an RSA key. The ES algorithms need an elliptic curve key: generate one with \"openssl ecparam\".";
        }

        if (family != "ES" && pem.Contains("EC PRIVATE KEY", StringComparison.Ordinal))
        {
            return "This is an elliptic curve key. The RS and PS algorithms need an RSA key: generate one with \"openssl genrsa\".";
        }

        return $"This does not read as a private key the {family} algorithms can use. Check that the whole PEM block was copied, including its BEGIN and END lines.";
    }

    private static bool LooksLikeRsa(string pem)
    {
        return pem.Contains("RSA PRIVATE KEY", StringComparison.Ordinal)
            || pem.Contains("BEGIN PRIVATE KEY", StringComparison.Ordinal);
    }

    // JWA ties each ES algorithm to one curve: ES256 to P-256, ES384 to P-384, ES512 to
    // P-521 (521, not 512). Signing a P-256 key with SHA-384 would produce a token that no
    // validator accepts, so those combinations are refused rather than silently produced.
    // RSA has no such rule: one RSA key signs all three hash sizes, which is exactly the
    // contrast between the two families.
    public string? Unsupported(int hashBits)
    {
        if (_ecdsa == null)
        {
            return null;
        }

        var required = hashBits == 512 ? 521 : hashBits;

        if (_ecdsa.KeySize == required)
        {
            return null;
        }

        return $"Needs a {CurveName(required)} key. This one is {CurveName(_ecdsa.KeySize)}, and the curve is what decides the hash size.";
    }

    public byte[] Sign(byte[] signingInput, int hashBits)
    {
        var hash = hashBits switch
        {
            384 => HashAlgorithmName.SHA384,
            512 => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256,
        };

        if (_ecdsa != null)
        {
            // SignData returns the raw r||s pair, which is the format JWS wants. The DER
            // encoding that OpenSSL prints elsewhere would not verify here.
            return _ecdsa.SignData(signingInput, hash);
        }

        return _rsa!.SignData(
            signingInput,
            hash,
            _usePss ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1);
    }

    private static string CurveName(int keySizeBits)
    {
        return keySizeBits switch
        {
            256 => "P-256",
            384 => "P-384",
            521 => "P-521",
            _ => $"{keySizeBits}-bit",
        };
    }

    public void Dispose()
    {
        _rsa?.Dispose();
        _ecdsa?.Dispose();
    }
}
