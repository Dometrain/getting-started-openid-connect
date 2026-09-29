namespace OidcDemoApp;

// Every value below points at the instructor's hosted authorization server and demo
// APIs, kept running for the lifetime of the course. There is no real secret here:
// the client_secret values are demo-only and are meant to be visible to students.
public static class Settings
{
    // The authorization server (Duende IdentityServer)
    public const string OIDCServer = "https://identityservice.secure.nu";
    // public const string OIDCServer = "https://localhost:6001";

    // Token-secured demo APIs
    public const string APIEndpoint = "https://www.secure.nu/tokenapi/getidentity";
    public const string GetTimeAPIEndpoint = "https://paymentapi.secure.nu/gettime";

    // The protected payment API (https://paymentapi.secure.nu). Its endpoints require
    // different things from the token, which is what makes them useful side by side:
    //   /identity            any authenticated caller, echoes back every claim
    //   /payments            requires scope=payment and role=finance (alice and bob)
    //   /payments/newfeature requires scope=payment and role=developer (bob only)
    // The guest user has no roles at all, so a guest token is accepted by /identity
    // and rejected by both payment endpoints. That is the point of that user.
    public const string PaymentApiIdentityEndpoint = "https://paymentapi.secure.nu/identity";
    public const string PaymentApiPaymentsEndpoint = "https://paymentapi.secure.nu/payments";
    public const string PaymentApiNewFeatureEndpoint = "https://paymentapi.secure.nu/payments/newfeature";

    // Typed HTTP client base address for the Duende access-token-managed "paymentapi" client
    public const string PaymentApiBaseUrl = "https://www.tn-data.se";

    // The audience the payment API expects in a token, and the symmetric key it accepts
    // HS256 tokens under. Used by the symmetric token generator (Features/Tools) so a
    // hand-signed token is accepted on the first click.
    //
    // Yes, this key is public. That is the lesson: anyone holding a shared secret can
    // mint tokens the API believes, which is why symmetric signing does not scale beyond
    // a single trusted party. The API accepts these tokens on purpose.
    //
    // It is exactly 32 bytes because HS256 keys must be at least as long as the SHA-256
    // output they produce. Shorter keys are refused outright by the API's validator.
    public const string PaymentApiAudience = "payment";
    public const string PaymentApiHs256Secret = "my-hs256-secret-must-be-32-bytes";

    // The issuer the asymmetric token generator stamps into its tokens. Those tokens are
    // minted by this application, not by the OpenID Provider, so claiming the provider's
    // issuer would be a lie on screen at the exact moment the page is teaching what a
    // signature proves. The symmetric generator still uses OIDCServer above, because the
    // payment API validates that issuer.
    //
    // A URL, because that is the shape of every other issuer in this course and what
    // OpenID Connect requires of an ID token issuer. Hardcoded to the local address on
    // purpose: these tokens are only ever read by a human or pasted into jwt.io, so the
    // value never has to resolve to anything, and a fixed string is one less moving part
    // on camera.
    public const string DemoAppIssuer = "https://localhost:5001";

    // The RS256 key pair used by the asymmetric token generator (Features/Tools). A
    // 2048-bit RSA pair, generated once and hardcoded here so the demo never depends on
    // key generation. Where these come from, and what is actually inside them, is the
    // next video's subject.
    //
    // Unlike the symmetric secret above, these are NOT known to the payment API and are
    // not meant to be. Tokens signed with this key are verified at jwt.io, by pasting the
    // public key in. The demo is the key split itself, not another API call.
    //
    // Note the difference from the symmetric secret. That one key does both jobs, so
    // publishing it hands out the power to mint tokens. Here only the private key signs.
    // The public key below verifies and nothing else, which is why it is safe to paste
    // into jwt.io, or into anyone else's hands, without giving anything away.
    //
    // PKCS#8 ("BEGIN PRIVATE KEY") and SubjectPublicKeyInfo ("BEGIN PUBLIC KEY"), the two
    // formats RSA.ImportFromPem reads and jwt.io accepts.
    public const string DemoRs256PrivateKey = """
        -----BEGIN PRIVATE KEY-----
        MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDHQQUS0w6cpLhV
        FUeeAFg9nfXBoYoFEbtUN3ax3rE8NL57DBdlNrNC/+VXyGnup4Qgm84JvMFvfPz5
        xltEWgvEDF5J//oRLw5btpktNJ3SIp7Iye8WYjQl9Tlb5s5H1uV6roZZWUUuc+4P
        AGjyjDX/+nVjaupt3SJk+Ksq1UVj3QJdE4PtbRkRCQrEtv7AILMj9TIWmKmFCbcj
        LwoKHwEPY8Bq92znzAbhs6K+Vi8BJLxaLfyVr83LmmRHVvj+b5gruGr1ocG2DEgk
        /BlZZtGUBUP9T25+kh1rtl3vwrEOfRsaAqIjkDxmZh7zZAjeiqoDB1jU1xPrln+B
        Avv403sVAgMBAAECggEASnnTG6Yn4ATxoVvC2Rvn36AbK7Tlkd9+1YulofZK6OYO
        DJAIbpxzhKeBYb5XWgzjJg0Whi03YCSnqfSqSHIf07eLxr0XCzU7eKxXtL3l/5VR
        JFvWMdejBk0Sk3dBwzhfrett7Beh9CsA4DqW/5HI3VUQa4ia91WgdX08/VWaCij4
        EHldBYhHIXQlFBeTwikHrd+0O4dZVwz2LHTUz3jC2Lqa87KPcKhBTdJ9W0eWaCLe
        6bht+ernpjakPDwAFJaLg16w2f7KJjCDgBTuytRxuz7XjZsYgAeYbKjlxpZn+rSm
        91AtJeV+w8ju1lk6TTg8rRam8D2bCouHzYyKJYMwQQKBgQDNbQTQhFeykRzK+rUf
        SujKmiTUqm2mfaBPNO01eJF/B33+q6mmTzPbtfaW6g5igSMkl9dq37lyOKyjKZjB
        1j7dG0pInWFmycRcv1zTal7jQC/EfHzHeY4/v323Ui9w1oYF1suOoHXNk473WWi3
        9O5uSdU4Fj9wzsw/4py/9AZIEwKBgQD4TwSxv/pY2pm+81fnjKlXfoicAl6hD188
        MLEt+0FnGCTBw9gxk9UDT6YwTfVYoC5U6Q3rT7QgM821NtJ8ixwpp3t3iBPVb8O4
        uc7wqJnxLF2+6c29T0F6eN+rTYVjJ3JM0Zqv9UfdlmWXTlFsRIvpEMeSiws6sAjC
        yNNXwRvlNwKBgFf3dzhTc7pjqZDCCw17ZnRbzanD0XCaGyvP3kQnfSaIbsu/dsZg
        5DQRl6bIU6Ca7BGfc/+wDhl7a3HzPhbDR+gm54hw3GTHwe51g6qEwl1N/yaEqGpD
        A0cJGSyHZQlM09dsQ36v98IwjegdwVoE6JURyNxbqo8D/zLcN4N7W7VBAoGAMa/O
        Jqtdsw37GeSjwGe7MxDU0TMAMIZc7jRoH+VZDwIbyNIhnroQM9tqS9wtrhBPdRo1
        eNK4HoF/NjXEJLvJZORopmCKZ3k31u0HZcy3ETVphNxQGQOU/KbXSvX2LQMlsg13
        QKJ6QPLpXT+Et39356k95isAkjvLZP6+m56+sD0CgYB97qdKAwT8KUFc746s2k+V
        Z7fIhA7AhFv/AfANmu7b5FHJHzc3l9Bug7h+7we4UeIaX/S6htRwiWffsoM7dNWV
        rvRBTuv4CVzUQ7z+L5ZWyIE1+tBzvNTTYbybMqrNYKI4cdKoogq8jYuNza3iv7qt
        43uMfZsF+5ijE8k07jtJkg==
        -----END PRIVATE KEY-----
        """;

    public const string DemoRs256PublicKey = """
        -----BEGIN PUBLIC KEY-----
        MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAx0EFEtMOnKS4VRVHngBY
        PZ31waGKBRG7VDd2sd6xPDS+ewwXZTazQv/lV8hp7qeEIJvOCbzBb3z8+cZbRFoL
        xAxeSf/6ES8OW7aZLTSd0iKeyMnvFmI0JfU5W+bOR9bleq6GWVlFLnPuDwBo8ow1
        //p1Y2rqbd0iZPirKtVFY90CXROD7W0ZEQkKxLb+wCCzI/UyFpiphQm3Iy8KCh8B
        D2PAavds58wG4bOivlYvASS8Wi38la/Ny5pkR1b4/m+YK7hq9aHBtgxIJPwZWWbR
        lAVD/U9ufpIda7Zd78KxDn0bGgKiI5A8ZmYe82QI3oqqAwdY1NcT65Z/gQL7+NN7
        FQIDAQAB
        -----END PUBLIC KEY-----
        """;

    // Implicit flow demo: Features/ImplicitFlow
    public const string ImplicitFlowClientId = "implicitflowclient";

    // Client credentials flow demo: Features/ClientCredentials
    public const string ClientCredentialsClientId = "clientcredentialsflow";
    public const string ClientCredentialsClientSecret = "mysecret";

    // Authorization code flow demo: Features/CodeFlow
    public const string CodeFlowClientId = "codeflowclient";
    public const string CodeFlowClientSecret = "mysecret";

    // Authorization code flow + PKCE demo: Features/CodeFlowPkce
    public const string CodeFlowPkceClientId = "codeflowclient-pkce";
    public const string CodeFlowPkceClientSecret = "mysecret";

    // Refresh token demo: Features/Refresh
    public const string RefreshClientId = "codeflowclient-refresh";
    public const string RefreshClientSecret = "mysecret";

    // Library-driven login (Microsoft.AspNetCore.Authentication.OpenIdConnect): Features/User
    public const string LibraryLoginClientId = "localhost-addoidc-client";
    public const string LibraryLoginClientSecret = "mysecret";

    // External login against Duende's public demo provider: Features/User.
    // This one is NOT hosted by the instructor. It is Duende's own always-on demo
    // server, used to show a login against a completely different OpenID Provider.
    // Its demo clients accept any redirect URI, so the callback paths below do not
    // need registering anywhere.
    public const string ExternalOidcScheme = "external-oidc";
    public const string ExternalOidcServer = "https://demo.duendesoftware.com";
    public const string ExternalOidcClientId = "interactive.confidential";
    public const string ExternalOidcClientSecret = "secret";
}
