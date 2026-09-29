using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OidcDemoApp.Extensions;
using System.Security.Cryptography;
using System.Text;

namespace OidcDemoApp.Features.Tools;

public class ToolsController : Controller
{
    [HttpGet]
    public IActionResult Base64()
    {
        return View(new Base64ViewModel());
    }

    [HttpPost]
    public IActionResult Base64Encode(Base64ViewModel model)
    {
        model.Output = null;
        model.Error = null;

        var bytes = Encoding.UTF8.GetBytes(model.Input ?? "");
        var base64 = Convert.ToBase64String(bytes);

        model.Output = model.UseBase64Url ? ToBase64Url(base64) : base64;

        return View(nameof(Base64), model);
    }

    [HttpPost]
    public IActionResult Base64Decode(Base64ViewModel model)
    {
        model.Output = null;
        model.Error = null;

        var raw = (model.Input ?? "").Trim();

        try
        {
            var base64 = model.UseBase64Url ? FromBase64Url(raw) : raw;
            var bytes = Convert.FromBase64String(base64);
            var text = Encoding.UTF8.GetString(bytes);

            model.Output = model.PrettyPrintJson ? PrettyPrintIfJson(text) : text;
        }
        catch (FormatException)
        {
            model.Error = "That doesn't look like valid Base64 text.";
        }

        return View(nameof(Base64), model);
    }

    [HttpGet]
    public IActionResult Hash()
    {
        return View(new HashViewModel());
    }

    // These three sizes are exactly the ones named in the JWT signing algorithms
    // (RS256/384/512, ES256/384/512, HS256/384/512), and SHA-256 is what PKCE and at_hash use.
    [HttpPost]
    public IActionResult Hash(HashViewModel model)
    {
        var bytes = Encoding.UTF8.GetBytes(model.Input ?? "");

        model.Results.Add(new HashResult("SHA-256", 256, Format(SHA256.HashData(bytes), model.Format)));
        model.Results.Add(new HashResult("SHA-384", 384, Format(SHA384.HashData(bytes), model.Format)));
        model.Results.Add(new HashResult("SHA-512", 512, Format(SHA512.HashData(bytes), model.Format)));

        return View(model);
    }

    [HttpGet]
    public IActionResult AtHash()
    {
        return View(new AtHashViewModel());
    }

    // Deliberately kept off the hash calculator above. at_hash is not one of the digests that
    // page produces: it is the left-most half of one, base64url encoded. Listing it as a fourth
    // row there would teach exactly the misconception this page exists to correct.
    //
    // The hash size follows the "alg" of the ID token the claim will sit in (OpenID Connect
    // Core 3.1.3.6). This page never sees that token, so it computes the SHA-256 case, which is
    // what RS256, ES256 and HS256 all imply, and says as much on screen.
    [HttpPost]
    public IActionResult AtHash(AtHashViewModel model)
    {
        // A token never contains whitespace, so one that arrived wrapped across several lines is
        // repaired rather than hashed as pasted. A wrong answer produced by the clipboard halfway
        // through a recording teaches the wrong lesson.
        var pasted = (model.AccessToken ?? "").Trim();
        var token = new string(pasted.Where(character => !char.IsWhiteSpace(character)).ToArray());

        if (token.Length == 0)
        {
            model.Error = "Paste an access token. Any access token works here, JWT or opaque.";

            return View(model);
        }

        // The spec says the ASCII representation of the access token. UTF-8 produces the same
        // octets for anything a real token can contain, and does not silently mangle a stray
        // character pasted in by accident.
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        var kept = digest[..(digest.Length / 2)];
        var discarded = digest[(digest.Length / 2)..];

        model.Result = new AtHashResult(
            token,
            token.Length != pasted.Length,
            Convert.ToHexString(kept),
            Convert.ToHexString(discarded),
            digest.Length,
            kept.Length,
            ToBase64Url(Convert.ToBase64String(kept)));

        return View(model);
    }

    [HttpGet]
    public IActionResult SymmetricToken()
    {
        return View(NewDefaultToken());
    }

    // Step one of the demo: base64url encode the two JSON documents and join them. No
    // secret and no signature yet, so the encoded segments and the signing input can be
    // read out loud on their own. Pressing this again after editing the JSON re-encodes,
    // and drops any signature produced from the previous version of the token.
    [HttpPost]
    public IActionResult EncodeSymmetricToken(SymmetricTokenViewModel model)
    {
        EncodeSegments(model);
        model.Stage = TokenBuildStage.Encoded;

        return View(nameof(SymmetricToken), model);
    }

    // Step two: sign. Signs the token by hand rather than through a JWT library. A library
    // would refuse a short key, refuse an edited "alg", and hide the string that actually
    // gets signed, which are the three things this page exists to show. The segments are
    // re-encoded first, so an edit made after step one is always picked up here.
    [HttpPost]
    public IActionResult SignSymmetricToken(SymmetricTokenViewModel model)
    {
        EncodeSegments(model);

        var signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(model.Secret ?? ""),
            Encoding.UTF8.GetBytes(model.SigningInput!));

        model.Signature = ToBase64Url(Convert.ToBase64String(signature));
        model.Token = $"{model.SigningInput}.{model.Signature}";
        model.Stage = TokenBuildStage.Complete;

        return View(nameof(SymmetricToken), model);
    }

    // Resets the form, which also stamps fresh iat/nbf/exp values so the token is not
    // born expired after the page has been sitting open during a recording.
    [HttpPost]
    public IActionResult ResetSymmetricToken()
    {
        return View(nameof(SymmetricToken), NewDefaultToken());
    }

    [HttpGet]
    public IActionResult AsymmetricToken()
    {
        return View(NewDefaultAsymmetricToken());
    }

    // The same step one as the symmetric page, deliberately. Encoding is identical between
    // HS256 and RS256, and seeing that the two pages behave the same right up to the moment
    // a key is used is what makes the next step land.
    [HttpPost]
    public IActionResult EncodeAsymmetricToken(AsymmetricTokenViewModel model)
    {
        EncodeSegments(model);
        model.Stage = TokenBuildStage.Encoded;

        return View(nameof(AsymmetricToken), model);
    }

    // Step two: sign with the private key alone. The public key is never touched here,
    // which is the entire point. RS256 is RSASSA-PKCS1-v1_5 over a SHA-256 hash of the
    // signing input, which is what SignData with Pkcs1 padding produces.
    [HttpPost]
    public IActionResult SignAsymmetricToken(AsymmetricTokenViewModel model)
    {
        EncodeSegments(model);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(Settings.DemoRs256PrivateKey);

        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(model.SigningInput!),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        model.Signature = ToBase64Url(Convert.ToBase64String(signature));
        model.Token = $"{model.SigningInput}.{model.Signature}";
        model.Stage = TokenBuildStage.Complete;

        return View(nameof(AsymmetricToken), model);
    }

    [HttpPost]
    public IActionResult ResetAsymmetricToken()
    {
        return View(nameof(AsymmetricToken), NewDefaultAsymmetricToken());
    }

    [HttpGet]
    public IActionResult Jwk()
    {
        return View(new JwkViewModel());
    }

    // OpenSSL has no PEM to JWK conversion, so this fills the gap between how the demo keys
    // are generated and how an OpenID Provider publishes them.
    [HttpPost]
    public IActionResult Jwk(JwkViewModel model)
    {
        var result = JwkConverter.TryConvert(model.Pem, out var error);

        model.Error = error;
        model.Description = result?.Description;
        model.Jwk = result?.Jwk;
        model.PrivateJwk = result?.PrivateJwk;
        model.Kid = result?.Kid;

        return View(model);
    }

    [HttpGet]
    public IActionResult CustomKeyToken()
    {
        return View(NewDefaultCustomKeyToken());
    }

    [HttpPost]
    public IActionResult EncodeCustomKeyToken(CustomKeyTokenViewModel model)
    {
        EncodeSegments(model);
        model.Stage = TokenBuildStage.Encoded;

        return View(nameof(CustomKeyToken), model);
    }

    // Signs the same payload three times, once per hash size in the chosen family, so the
    // three can be read side by side. Each one gets its own header, because "alg" has to
    // name the algorithm that actually signed the token or nothing will verify it. That is
    // also why the three tokens differ in their first segment but share their second.
    [HttpPost]
    public IActionResult SignCustomKeyToken(CustomKeyTokenViewModel model)
    {
        EncodeSegments(model);
        model.Stage = TokenBuildStage.Encoded;

        using var key = SigningKey.TryLoad(model.PrivateKey, model.Family, out var keyError);
        model.KeyError = keyError;
        model.KeyDescription = key?.Description;

        if (key == null)
        {
            return View(nameof(CustomKeyToken), model);
        }

        foreach (var hashBits in new[] { 256, 384, 512 })
        {
            model.Variants.Add(SignVariant(model, key, hashBits));
        }

        model.Stage = TokenBuildStage.Complete;

        return View(nameof(CustomKeyToken), model);
    }

    [HttpPost]
    public IActionResult ResetCustomKeyToken()
    {
        return View(nameof(CustomKeyToken), NewDefaultCustomKeyToken());
    }

    private static SignedVariant SignVariant(CustomKeyTokenViewModel model, SigningKey key, int hashBits)
    {
        var algorithm = $"{model.Family}{hashBits}";

        if (key.Unsupported(hashBits) is { } reason)
        {
            return new SignedVariant(algorithm, null, null, null, null, reason);
        }

        var header = WithAlgorithm(model.Header, algorithm);
        var encodedHeader = EncodeSegment(header, out _);
        var signingInput = $"{encodedHeader}.{model.EncodedPayload}";

        var signature = key.Sign(Encoding.UTF8.GetBytes(signingInput), hashBits);
        var encodedSignature = ToBase64Url(Convert.ToBase64String(signature));

        return new SignedVariant(
            algorithm,
            encodedHeader,
            encodedSignature,
            signature.Length,
            $"{signingInput}.{encodedSignature}",
            null);
    }

    // Rewrites the "alg" claim to name the algorithm that is about to sign. A header left
    // saying something else produces a token every validator rejects, which is a different
    // lesson (video 4) and not the one this page is for. Anything that is not a JSON object
    // is handed back untouched; EncodeSegment already warns about that.
    private static string WithAlgorithm(string? header, string algorithm)
    {
        try
        {
            var json = JObject.Parse((header ?? "").Trim());
            json["alg"] = algorithm;

            return json.ToString(Formatting.Indented);
        }
        catch (JsonException)
        {
            return header ?? "";
        }
    }

    private static CustomKeyTokenViewModel NewDefaultCustomKeyToken()
    {
        var now = DateTimeOffset.UtcNow;

        // The private key is deliberately left empty. Pasting one in is the demo: the key
        // comes from OpenSSL, outside this application, and this page only signs with it.
        return new CustomKeyTokenViewModel
        {
            Header = """
                {
                  "alg": "RS256",
                  "typ": "JWT"
                }
                """,
            Payload = $$"""
                {
                  "iss": "{{Settings.DemoAppIssuer}}",
                  "aud": "{{Settings.PaymentApiAudience}}",
                  "sub": "alice",
                  "name": "Alice Smith",
                  "scope": "payment",
                  "role": "finance",
                  "iat": {{now.ToUnixTimeSeconds()}},
                  "nbf": {{now.ToUnixTimeSeconds()}},
                  "exp": {{now.AddHours(1).ToUnixTimeSeconds()}}
                }
                """,
        };
    }

    // The signing input is the whole point of both pages. The signing algorithm never sees
    // the JSON and the key side by side: it signs this one joined string.
    private static void EncodeSegments(TokenBuilderViewModel model)
    {
        model.EncodedHeader = EncodeSegment(model.Header, out var headerWarning);
        model.HeaderWarning = headerWarning;

        model.EncodedPayload = EncodeSegment(model.Payload, out var payloadWarning);
        model.PayloadWarning = payloadWarning;

        model.SigningInput = $"{model.EncodedHeader}.{model.EncodedPayload}";
    }

    // The two pages below deliberately spell their own defaults out in full rather than
    // sharing a helper. They are teaching material for two different videos, and each one
    // should be free to change without silently rewriting the other page's token.
    //
    // Both stamp iat/nbf/exp fresh on every GET (and on "Reset to defaults"), so a page
    // left open during a recording does not quietly produce an already-expired token.

    // These claims are exactly what the payment API asks for: the issuer and audience it
    // validates, plus the scope and role that /payments checks. Sign, and the token works
    // without editing anything. Change one value and watch the API start refusing it,
    // which is the demo.
    private static SymmetricTokenViewModel NewDefaultToken()
    {
        var now = DateTimeOffset.UtcNow;

        return new SymmetricTokenViewModel
        {
            Header = """
                {
                  "alg": "HS256",
                  "typ": "JWT"
                }
                """,
            Payload = $$"""
                {
                  "iss": "{{Settings.OIDCServer}}",
                  "aud": "{{Settings.PaymentApiAudience}}",
                  "sub": "alice",
                  "name": "Alice Smith",
                  "scope": "payment",
                  "role": "finance",
                  "iat": {{now.ToUnixTimeSeconds()}},
                  "nbf": {{now.ToUnixTimeSeconds()}},
                  "exp": {{now.AddHours(1).ToUnixTimeSeconds()}}
                }
                """,
            Secret = Settings.PaymentApiHs256Secret,
        };
    }

    // The same claims as the symmetric page, so the two can be read side by side, with two
    // deliberate differences. "alg" is RS256, which is the subject of the video. And "iss"
    // is this application's own address rather than the OpenID Provider's, because that is
    // the truth: this token is minted here, and it is never sent to the payment API.
    private static AsymmetricTokenViewModel NewDefaultAsymmetricToken()
    {
        var now = DateTimeOffset.UtcNow;

        return new AsymmetricTokenViewModel
        {
            Header = """
                {
                  "alg": "RS256",
                  "typ": "JWT"
                }
                """,
            Payload = $$"""
                {
                  "iss": "{{Settings.DemoAppIssuer}}",
                  "aud": "{{Settings.PaymentApiAudience}}",
                  "sub": "alice",
                  "name": "Alice Smith",
                  "scope": "payment",
                  "role": "finance",
                  "iat": {{now.ToUnixTimeSeconds()}},
                  "nbf": {{now.ToUnixTimeSeconds()}},
                  "exp": {{now.AddHours(1).ToUnixTimeSeconds()}}
                }
                """,
        };
    }

    // Valid JSON is re-serialized compactly, which is what a real token segment looks like
    // and keeps the encoded string short enough to read on screen. Anything that is not
    // valid JSON is encoded exactly as typed rather than throwing: editing the header into
    // something broken should produce a token the API rejects, not an error page.
    private static string EncodeSegment(string? json, out string? warning)
    {
        warning = null;

        var text = (json ?? "").Trim();

        try
        {
            text = JToken.Parse(text).ToString(Formatting.None);
        }
        catch (JsonReaderException)
        {
            warning = "This is not valid JSON. It is encoded exactly as typed, so the API will reject the token.";
        }

        return ToBase64Url(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
    }

    private static string Format(byte[] hash, HashOutputFormat format)
    {
        return format switch
        {
            HashOutputFormat.Base64 => Convert.ToBase64String(hash),
            HashOutputFormat.Base64Url => ToBase64Url(Convert.ToBase64String(hash)),
            _ => Convert.ToHexString(hash),
        };
    }

    // JWT payloads and userinfo responses decode to JSON, so indenting them makes them
    // far easier to read on screen. Anything that is not a JSON object is left untouched.
    private static string PrettyPrintIfJson(string text)
    {
        if (!text.TrimStart().StartsWith('{'))
        {
            return text;
        }

        try
        {
            return text.BeautifyJson();
        }
        catch (JsonException)
        {
            // Looked like JSON, was not. Show the raw decoded text instead.
            return text;
        }
    }

    // Base64url per RFC 4648: '+' -> '-', '/' -> '_', padding stripped.
    private static string ToBase64Url(string base64)
    {
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string FromBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');

        var padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 += new string('=', 4 - padding);
        }

        return base64;
    }
}
