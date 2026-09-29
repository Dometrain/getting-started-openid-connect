using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Extensions;
using OidcDemoApp.Models;

namespace OidcDemoApp.Features.ClientCredentials;

public class ClientCredentialsController : Controller
{
    private readonly ILogger<ClientCredentialsController> _logger;

    public ClientCredentialsController(ILogger<ClientCredentialsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new ClientCredentialRequest());
    }


    [HttpPost]
    public async Task<IActionResult> GetAccessToken(string useaccesstoken, ClientCredentialRequest data)
    {
        if (useaccesstoken == null)
        {
            //Get access token

            var url = new Url(Settings.OIDCServer + "/connect/token");

            var token = await url.PostUrlEncodedAsync(new
            {
                grant_type = data.GrantType,
                client_id = data.ClientId,
                client_secret = data.ClientSecret,
                scope = data.Scope,

            }).ReceiveJson<OidcTokenResponse>();

            data.Token = token;
            data.ApiResult = "";

            ModelState.Clear();
            return View("Index", data);
        }

        else
        {
            //Use access token

            try
            {
                //Access the payment API using the access token

                if (data.Token != null)
                {
                    string ApiResult = await Settings.APIEndpoint
                                            .WithOAuthBearerToken(data.Token?.AccessToken)
                                            .GetStringAsync();

                    data.ApiResult = ApiResult.BeautifyJson();
                }
                else
                {
                    data.ApiResult = "";
                }
            }
            catch (Exception ex)
            {
                data.ApiResult = ex.ToString();
            }


            ModelState.Clear();

            return View("Index", data);
        }

    }

}