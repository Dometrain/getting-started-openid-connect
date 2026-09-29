using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OidcDemoApp.Features.Tools;

// Turns a PEM key into a JWK. OpenSSL will not do this, which is why the tool exists: the
// keys in the demos are generated with OpenSSL, but an OpenID Provider publishes them as
// JSON at its jwks_uri, and the two formats hold exactly the same numbers.
//
// A JWK can hold a private key too (RFC 7518: "d" for EC, and "d", "p", "q", "dp", "dq",
// "qi" for RSA), which is how signing keys are usually configured and how a client doing
// private_key_jwt holds its own key. What is public-only is the JWKS a provider publishes,
// not the format. Hence both forms here, with the publishable one as the default answer.
public static class JwkConverter
{
    public sealed record Result(
        string Description,
        string Jwk,
        string? PrivateJwk,
        string Kid);

    public static Result? TryConvert(string? pem, out string? error)
    {
        error = null;

        var text = (pem ?? "").Trim();

        if (text.Length == 0)
        {
            error = "Paste a key in PEM format. A private or a public key both work.";
            return null;
        }

        if (text.Contains("ENCRYPTED", StringComparison.Ordinal))
        {
            error = "This key is passphrase protected. Decrypt it with OpenSSL first.";
            return null;
        }

        var fromPrivate = text.Contains("PRIVATE KEY", StringComparison.Ordinal);

        return TryRsa(text, fromPrivate) ?? TryEllipticCurve(text, fromPrivate, ref error);
    }

    private static Result? TryRsa(string pem, bool wantPrivate)
    {
        using var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception e) when (e is ArgumentException or CryptographicException)
        {
            return null;
        }

        var pub = rsa.ExportParameters(false);

        var n = ToBase64Url(pub.Modulus!);
        var e2 = ToBase64Url(pub.Exponent!);

        // RFC 7638: the thumbprint is a SHA-256 over the required members only, with no
        // whitespace and the names in lexicographic order. Those members are public ones,
        // so the private and public forms of one key share a kid, which is the point.
        var kid = Thumbprint($"{{\"e\":\"{e2}\",\"kty\":\"RSA\",\"n\":\"{n}\"}}");

        // No "alg" here on purpose. It is optional, and one RSA key is valid for all six of
        // RS256/384/512 and PS256/384/512, so naming one would claim something untrue.
        JObject Public() => new()
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["kid"] = kid,
            ["n"] = n,
            ["e"] = e2,
        };

        string? privateJwk = null;

        if (wantPrivate)
        {
            var full = rsa.ExportParameters(true);
            var jwk = Public();

            // RFC 7518 section 6.3.2. The other prime members are optional on their own, but
            // a producer including any of them should include them all.
            jwk["d"] = ToBase64UrlUInt(full.D!);
            jwk["p"] = ToBase64UrlUInt(full.P!);
            jwk["q"] = ToBase64UrlUInt(full.Q!);
            jwk["dp"] = ToBase64UrlUInt(full.DP!);
            jwk["dq"] = ToBase64UrlUInt(full.DQ!);
            jwk["qi"] = ToBase64UrlUInt(full.InverseQ!);

            privateJwk = jwk.ToString(Formatting.Indented);
        }

        return Build($"RSA, {rsa.KeySize}-bit", Public(), privateJwk, kid);
    }

    private static Result? TryEllipticCurve(string pem, bool wantPrivate, ref string? error)
    {
        using var ecdsa = ECDsa.Create();

        try
        {
            ecdsa.ImportFromPem(pem);
        }
        catch (Exception e) when (e is ArgumentException or CryptographicException)
        {
            error = "This does not read as an RSA or an elliptic curve key. Check that the whole PEM block was copied, including its BEGIN and END lines.";
            return null;
        }

        var pub = ecdsa.ExportParameters(false);

        var curve = CurveName(ecdsa.KeySize);
        if (curve == null)
        {
            error = $"This is a {ecdsa.KeySize}-bit curve. JWK only names P-256, P-384 and P-521.";
            return null;
        }

        // x and y are fixed width for the curve, left padded with zeros. Trimming them, which
        // is tempting because the numbers are the same, produces a JWK other libraries reject.
        var fieldBytes = (ecdsa.KeySize + 7) / 8;

        var x = ToBase64Url(PadLeft(pub.Q.X!, fieldBytes));
        var y = ToBase64Url(PadLeft(pub.Q.Y!, fieldBytes));

        var kid = Thumbprint($"{{\"crv\":\"{curve}\",\"kty\":\"EC\",\"x\":\"{x}\",\"y\":\"{y}\"}}");

        // Unlike RSA, "alg" can be stated here: the curve decides it. P-256 signs with ES256
        // and nothing else, which is the same rule the signing page enforces.
        JObject Public() => new()
        {
            ["kty"] = "EC",
            ["use"] = "sig",
            ["alg"] = AlgorithmFor(curve),
            ["kid"] = kid,
            ["crv"] = curve,
            ["x"] = x,
            ["y"] = y,
        };

        string? privateJwk = null;

        if (wantPrivate)
        {
            var full = ecdsa.ExportParameters(true);
            var jwk = Public();

            // RFC 7518 section 6.2.2.1. Unlike RSA's private members, "d" here is fixed width
            // for the curve, the same as x and y.
            jwk["d"] = ToBase64Url(PadLeft(full.D!, fieldBytes));

            privateJwk = jwk.ToString(Formatting.Indented);
        }

        return Build($"EC, {curve}", Public(), privateJwk, kid);
    }

    // Deliberately no JWKS output. The page can produce a private JWK, and printing a
    // "keys" array beside one would suggest a private key belongs in a published key set.
    // The real key set is better shown against a provider's live jwks_uri anyway.
    private static Result Build(string description, JObject jwk, string? privateJwk, string kid)
    {
        return new Result(
            description,
            jwk.ToString(Formatting.Indented),
            privateJwk,
            kid);
    }

    private static string Thumbprint(string canonicalJson)
    {
        return ToBase64Url(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson)));
    }

    private static byte[] PadLeft(byte[] value, int length)
    {
        if (value.Length >= length)
        {
            return value;
        }

        var padded = new byte[length];
        value.CopyTo(padded, length - value.Length);

        return padded;
    }

    private static string? CurveName(int keySizeBits)
    {
        return keySizeBits switch
        {
            256 => "P-256",
            384 => "P-384",
            521 => "P-521",
            _ => null,
        };
    }

    private static string AlgorithmFor(string curve)
    {
        return curve switch
        {
            "P-384" => "ES384",
            "P-521" => "ES512",
            _ => "ES256",
        };
    }

    private static string ToBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    // Base64urlUInt (RFC 7515 section 2): the unsigned big-endian value with no leading zero
    // octets. .NET hands back fixed width buffers, which can carry a leading zero.
    private static string ToBase64UrlUInt(byte[] value)
    {
        var start = 0;
        while (start < value.Length - 1 && value[start] == 0)
        {
            start++;
        }

        return ToBase64Url(value[start..]);
    }
}
