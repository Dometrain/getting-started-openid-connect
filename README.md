# Getting Started: OpenID Connect
This repository contains starter files and complete sample solutions for the [Getting Started: OpenID Connect](https://dometrain.com/) course on [Dometrain](https://dometrain.com/). 
The course covers how to integrate OpenID Connect into your applications, including authentication flows, tokens, and real-world 
implementation patterns.

## Included Projects
* **Demo Application**  
  The ASP.NET Core project used throughout the course.

## Settings and Configuration
* **[Settings.md](Settings.md)**  
  All client IDs, secrets, URLs, and configuration values used throughout the course.

## Links and Resources
* **[Resources.md](Resources.md)**  
  All external references mentioned throughout the course.

## Updates and Errata
* **[CHANGELOG.md](CHANGELOG.md)**  
  All course updates.
* **[ERRATA.md](ERRATA.md)**  
  Known errors and corrections.

## Support and Bug Reports
* **An online service is down** (the authorization server or the demo API)  
  Email tore@tn-data.se directly so it can be fixed as soon as possible.
* **Anything else** (a bug in the sample code, or a general question)  
  Email tore@tn-data.se or post an issue in the course material GitHub repository.

## Author
**Tore Nestenius** is an independent consultant, trainer, and developer specializing in 
security, authentication, and .NET. He is also a Microsoft .NET MVP. Feel free to reach out or follow along:

* [Blog](https://nestenius.se)
* [Web](https://tn-data.se)
* [LinkedIn](https://www.linkedin.com/in/torenestenius/)

## Other Courses
* **[Getting Started: Authentication and Authorization in .NET](https://dometrain.com/course/getting-started-authentication-and-authorization-in-dotnet/)**  
  Learn how to get started with authentication and authorization in .NET

## Frequently Asked Questions

### Requests to localhost do not show up in Fiddler
By default, Firefox does not send localhost traffic through a proxy, so Fiddler never sees it. Traffic to sites on the internet still appears as usual, which makes this easy to miss.

To change this, open a new tab in Firefox and go to `about:config`, search for the `network.proxy.allow_hijacking_localhost` preference, and set it to `true`. Then reload the page.

This is also covered in the **Installing Fiddler** video in the *Getting Started* section. Other browsers can bypass the proxy for localhost in the same way, so if you are not using Firefox and localhost traffic is missing, this is the first thing to check.

### Fiddler only shows "Tunnel to" entries
Fiddler is capturing the traffic, but it cannot look inside it yet. Enable **Decrypt HTTPS Traffic** under Tools, Options, HTTPS, and then restart both Fiddler and the browser.

This is covered in the **Fiddler and HTTPS** video in the *Getting Started* section.

### The native application demo fails to start on port 5001
The console application in the [Demo Files](Demo%20Files) folder listens on `https://localhost:5001`, which is the same address the web demo application uses. Only one of them can run at a time, so stop the web demo application before starting the console application.

If the browser shows "Unable to connect" after you have signed in, the console application is no longer running. It stops listening as soon as it has received the authorization code, so every sign in needs a freshly started application.

### The browser warns about the certificate when the native application demo redirects back
The console application serves `https://localhost:5001` using the ASP.NET Core development certificate. If that certificate is not installed and trusted on your machine, the browser shows a certificate warning instead of the "Sign in complete" page.

Run `dotnet dev-certs https --trust` once, and then try again.

On Linux, trusting the development certificate is only supported on newer .NET versions, and the browser certificate stores also need the `certutil` tool (the `libnss3-tools` package on Debian and Ubuntu).

