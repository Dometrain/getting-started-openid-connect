@echo off
setlocal

rem Trust the local HTTPS development certificate (one-time; safe to run every time).
dotnet dev-certs https --trust

dotnet run --project OidcDemoApp --launch-profile https

endlocal
