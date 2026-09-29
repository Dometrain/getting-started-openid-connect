using System.ComponentModel.DataAnnotations;

namespace OidcDemoApp.Features.Tools;

// Both token generators are walked one step at a time rather than producing everything at
// once, so each stage can be talked about before the next one appears. Ordered, so the
// views can test with >=.
public enum TokenBuildStage
{
    NotEncoded = 0,
    Encoded = 1,

    // The token is signed and complete. Named for the result rather than the act, because
    // an enum member called "Signed" trips the "identifier contains a type name" analyzer.
    Complete = 2,
}

// Everything the symmetric (HS256) and asymmetric (RS256) generators have in common: the
// same two JSON documents in, the same base64url pipeline, the same staged reveal. What
// differs is only the key material and how the signature is produced, which is exactly the
// comparison the two videos are built around.
public abstract class TokenBuilderViewModel
{
    [Display(Name = "Header")]
    [DataType(DataType.MultilineText)]
    public string Header { get; set; } = "";

    [Display(Name = "Payload")]
    [DataType(DataType.MultilineText)]
    public string Payload { get; set; } = "";

    // How far the student has walked the pipeline. Set by the controller on every post,
    // never trusted from the form.
    public TokenBuildStage Stage { get; set; }

    // Every intermediate value is kept so the page can show the same pipeline the slides
    // draw: encode each part, join them, then sign that one string.
    public string? EncodedHeader { get; set; }
    public string? EncodedPayload { get; set; }
    public string? SigningInput { get; set; }
    public string? Signature { get; set; }
    public string? Token { get; set; }

    public string? HeaderWarning { get; set; }
    public string? PayloadWarning { get; set; }
}
