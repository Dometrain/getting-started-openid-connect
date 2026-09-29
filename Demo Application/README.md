# OIDC Demo Client

This is the demo client for the course **Getting Started: OpenID Connect**. It
walks through the OAuth 2.0 / OpenID Connect flows step by step, building the raw HTTP
requests by hand so you can see exactly what goes over the wire.

There is nothing to install, configure, or fill in. Everything talks to a hosted
authorization server and a couple of demo APIs, so you can build and run the app as-is.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Running it

1. Create the local HTTPS development certificate (one time only):

   ```
   dotnet dev-certs https
   ```

   On Windows and macOS, also trust it, so the browser stops warning about it:

   ```
   dotnet dev-certs https --trust
   ```

   Trusting is not supported on Linux. Expect a certificate warning in the browser
   there, which is safe to click through for a localhost demo.

   Both launcher scripts in step 2 already do all of this for you. You only need to
   run these commands yourself if you start the app some other way.

2. Run the app:
   - **Windows:** double-click `RunApplication.bat`
   - **Linux / macOS:** `./RunApplication.sh`

3. Open **https://localhost:5001** in your browser.

Both scripts just run `dotnet run --project OidcDemoApp --launch-profile https`, so you
can also run that command directly, or open `OidcDemoApp.sln` in Visual Studio and press
F5.

If anything goes wrong, see [Troubleshooting](#troubleshooting) below.

## What's inside

The home page links to everything, grouped the same way as the navigation bar:

- **Flows**: implicit flow (fragment and form post), authorization code flow, authorization
  code flow with PKCE, client credentials flow, and refresh tokens.
- **Tools**: paste an access token to call the userinfo endpoint or a protected API, a
  URL query visualizer for reading redirect URLs, and a back-channel log that shows every
  HTTP request and response the app makes behind the scenes.

Each flow page builds the request one field at a time so you can see what every OAuth/OIDC
parameter does before the app sends it.

## Troubleshooting

Roughly in the order people run into them.

**`/usr/bin/env: 'bash\r': No such file or directory`** (Linux or macOS)

The script picked up Windows line endings somewhere along the way, usually from being
zipped and unzipped on Windows. Strip the carriage returns:

```
sed -i 's/\r$//' RunApplication.sh
```

Or ignore the script and run the app directly:

```
dotnet run --project OidcDemoApp --launch-profile https
```

**`Permission denied` when running `./RunApplication.sh`** (Linux or macOS)

The executable bit was lost, which also happens when unzipping. Restore it, or let bash
run the file instead:

```
chmod +x RunApplication.sh
```

```
bash RunApplication.sh
```

**`dotnet: command not found`**

The .NET SDK is not installed, or it is not on your PATH. Install the
[.NET 10 SDK](https://dotnet.microsoft.com/download) and open a new terminal, because a
freshly installed SDK is not visible in terminals that were already open.

**`The current .NET SDK does not support targeting .NET 10.0`**

You have an older SDK. Check what is installed:

```
dotnet --list-sdks
```

You need a 10.x entry in that list. The runtime alone is not enough, it has to be the SDK.

**The browser warns that the connection is not private**

The app runs on HTTPS with a self-signed development certificate, so the warning is about
that certificate and not about anything being wrong.

On Windows and macOS, trust it once:

```
dotnet dev-certs https --trust
```

Then restart the browser completely, since browsers cache certificate decisions for the
lifetime of the process. If the warning survives that, reset the certificate and trust it
again:

```
dotnet dev-certs https --clean
```

On Linux, `dotnet dev-certs https --trust` is not supported. The warning is expected
there, and clicking through it is safe for a localhost demo.

**`Failed to bind to address https://localhost:5001`**

Something else is already using port 5001, often an earlier run of this same app that did
not shut down. Find it and stop it.

On Windows:

```
netstat -ano | findstr :5001
```

On Linux or macOS:

```
lsof -i :5001
```

Do not work around this by moving the app to a different port. The hosted authorization
server only accepts redirect URIs on `https://localhost:5001`, so the flows will fail with
an invalid redirect URI error on any other port.

**The app starts but no browser window opens**

Open **https://localhost:5001** yourself. The browser only launches automatically when the
app is started through its launch profile, and even then some environments block it.

**A flow fails with a timeout or a connection error**

The demo is not self-contained, it talks to a hosted authorization server and a set of
hosted APIs. These hosts have to be reachable:

- `identityservice.secure.nu` (the authorization server)
- `paymentapi.secure.nu`, `www.secure.nu`, and `www.tn-data.se` (the demo APIs)

On a corporate network, a proxy or firewall is the usual cause. Check the back-channel log
under **Tools**, which shows every request the app made behind the scenes along with what
came back, so you can see exactly which call failed.

**`dotnet restore` or the first build fails to download packages**

The build needs to reach `api.nuget.org` to restore the NuGet packages. Same story as
above: a proxy or an offline machine will stop it.

## Finding your way around

The project follows a feature folder layout. Each flow and each tool lives in its own
folder under `OidcDemoApp/Features`, holding the controller and the Razor views for that
one topic, so the code for anything you see in the navigation bar is in the folder with
the matching name.

Two files are worth knowing about:

- `OidcDemoApp/Settings.cs` collects every endpoint and client value the demo uses, each
  one commented, so you can see the whole configuration in one place.
- `OidcDemoApp/Infrastructure/BackChannelLogger` is what captures the raw HTTP traffic
  behind the **Tools > back-channel log** page.
