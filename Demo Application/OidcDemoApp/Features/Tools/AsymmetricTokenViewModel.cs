namespace OidcDemoApp.Features.Tools;

public class AsymmetricTokenViewModel : TokenBuilderViewModel
{
    // The keys are read-only on the page and never posted back, so they are read straight
    // from Settings rather than round-tripping through the form. Editing them is not the
    // demo: the demo is that two different keys do two different jobs.
    public static string PrivateKeyPem => Settings.DemoRs256PrivateKey;

    public static string PublicKeyPem => Settings.DemoRs256PublicKey;
}
