#!/usr/bin/env bash
set -e

# Create the local HTTPS development certificate if it does not already exist.
dotnet dev-certs https >/dev/null

# Trust it in the OS certificate store. Supported on Windows and macOS.
# Not supported on Linux by dotnet dev-certs; the browser will show a certificate
# warning on first run there, which is expected for a localhost demo.
dotnet dev-certs https --trust >/dev/null 2>&1 || true

dotnet run --project OidcDemoApp --launch-profile https
