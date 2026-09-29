	# Settings and Configuration

This file is a reference for all settings, configuration values, client credentials, and URLs
used throughout the **Getting Started: OpenID Connect** course on [Dometrain](https://dometrain.com/).

---

## Authorization Server

| Setting | Value |
|---------|-------|
| Base URL | https://identityservice.secure.nu/ |

## Online Demo Client
Only use this one if you can't install the client locally. Some demos will not work in this version.

| Setting | Value |
|---------|-------|
| URL | https://oidcclient.secure.nu |

---

## Test Users

| Username | Password | Notes |
|----------|----------|-------|
| alice | alice | Contractor, single role (finance) |
| bob | bob | Employee, multiple roles (ceo, finance, developer) |
| guest | guest | No roles and no claims beyond the name (a valid login that still gets turned away by the protected APIs) |
| large | large | A user with a long claim value (tests token/cookie size limits) |

---

## Configured Clients

### Implicit Flow
Default implicit client with no client secret.

| Setting | Value |
|---------|-------|
| Client_id | implicitflowclient |
| Allowed Scopes | openid email profile phone address employee_info api payment |
| Redirect URI | /ImplicitFlow/LoggedInUsingFragment<br>/ImplicitFlow/LoggedInUsingPostBack |
| AccessToken Lifetime | 1 hour |
| AccessToken Format | JWT |

### Implicit Flow Using Reference Token
Same as the implicitflowclient, but returns the access token as a reference token.

| Setting | Value |
|---------|-------|
| Client_id | implicit-referencetoken |
| Allowed Scopes | openid email profile phone address employee_info api payment |
| Redirect URI | /ImplicitFlow/LoggedInUsingFragment<br>/ImplicitFlow/LoggedInUsingPostBack |
| AccessToken Lifetime | 1 hour |
| AccessToken Format | Reference Token |

### Implicit Flow Without ID Token Claims
Same as the implicitflowclient, but returns no user claims in the ID token.

| Setting | Value |
|---------|-------|
| Client_id | implicit-noidtokenclaims |
| Allowed Scopes | openid email profile phone address employee_info api payment |
| Redirect URI | /ImplicitFlow/LoggedInUsingFragment<br>/ImplicitFlow/LoggedInUsingPostBack |
| AccessToken Lifetime | 1 hour |
| AccessToken Format | JWT |

### Authorization Code Flow Client
Client using the Authorization Code Flow.

| Setting | Value |
|---------|-------|
| Client_id | codeflowclient |
| Client_secret | mysecret |
| Allowed Scopes | openid email profile phone address offline_access employee_info api payment |
| Redirect URI | /codeflow/callback<br>/signin-oidc |
| Post Logout URI | /signout-callback-oidc |
| AccessToken Lifetime | 1 hour |
| AccessToken Format | JWT |
| Refresh Token Usage | One time only |

### Authorization Code Flow Client + PKCE
Client using the Authorization Code Flow and requires PKCE.

| Setting | Value |
|---------|-------|
| Client_id | codeflowclient-pkce |
| Client_secret | mysecret |
| Allowed Scopes | openid email profile phone address offline_access employee_info api payment |
| Redirect URI | /codeflowpkce/callback<br>/signin-oidc |
| Post Logout URI | /signout-callback-oidc |
| AccessToken Lifetime | 1 hour |
| AccessToken Format | JWT |
| Refresh Token Usage | One time only |

### Authorization Code Flow Client + Refresh Token
Client using the Authorization Code Flow and supports refresh token. PKCE is not required.

| Setting | Value |
|---------|-------|
| Client_id | codeflowclient-refresh |
| Client_secret | mysecret |
| Allowed Scopes | openid email profile phone offline_access employee_info api payment |
| Redirect URI | /codeflow/callback<br>/refresh/callback<br>/signin-oidc |
| Post Logout URI | /signout-callback-oidc |
| AccessToken Lifetime | 15 seconds |
| AccessToken Format | JWT |
| Refresh Token Usage | One time only |

### Client Credentials Flow
Client using the Client Credentials Flow.

| Setting | Value |
|---------|-------|
| Client_id | clientcredentialsflow |
| Client_secret | mysecret |
| Allowed Scopes | openid email profile phone address employee_info api payment |
| AccessToken Lifetime | 1 Hour |
| AccessToken Format | JWT |

### Library-Driven Login
Client used for the built-in ASP.NET Core OpenID Connect middleware login (Microsoft.AspNetCore.Authentication.OpenIdConnect), instead of a hand-built flow.

| Setting | Value |
|---------|-------|
| Client_id | localhost-addoidc-client |
| Client_secret | mysecret |
| Allowed Scopes | openid email profile phone address offline_access employee_info api payment |
| Redirect URI | /signin-oidc |
| Post Logout URI | /signout-callback-oidc |
| AccessToken Lifetime | 1 hour |
| AccessToken Format | JWT |

---

## External Login Provider

Used for the external login demo, showing a login against a completely different OpenID
Provider. This is Duende's own public demo authorization server, not the instructor's server
above, and is not maintained by the instructor.

| Setting | Value |
|---------|-------|
| Base URL | https://demo.duendesoftware.com |
| Client_id | interactive.confidential |
| Client_secret | secret |

---

## Demo APIs

### Payment
| Setting | Value |
|---------|-------|
| Base URL | https://paymentapi.secure.nu |
| Client_id | payment |
| Client_secret | mysecret |

### Invoice
Sample API, registered in the OpenID Provider, but not implemented.

| Setting | Value |
|---------|-------|
| Client_id | invoice |

### Order
Sample API, registered in the OpenID Provider, but not implemented.

| Setting | Value |
|---------|-------|
| Client_id | order |

