using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ListClient_OIDC;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace ListClient_OIDC.Controllers
{

    [Authorize]
    public class GraphController : Controller
    { 

        private IConfiguration _configuration;

        public GraphController(IConfiguration config) {
            _configuration = config;
        }

        public async Task<IActionResult> Index()
        {
            string userObjectID = (User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;

            string authority = $"{_configuration["AzureAd:Instance"]}{_configuration["AzureAd:TenantId"]}";
            string client_id = _configuration["AzureAd:ClientId"];
            string client_secret = _configuration["AzureAd:ClientSecret"];

            try {
                AuthenticationContext authContext = new AuthenticationContext(authority, new NaiveSessionCache(userObjectID, HttpContext.Session));
                ClientCredential credential = new ClientCredential(client_id, client_secret);
                AuthenticationResult result = await authContext.AcquireTokenSilentAsync("https://graph.microsoft.com", credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
                var cont = await response.Content.ReadAsStringAsync();
 
                ViewData["me"] = cont;
            } catch {
                ViewData["me"] = "Failed to access MS Graph";   
            }
    

            return View();
        }
    }
}