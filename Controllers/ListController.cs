using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using ListClient_OIDC;
using ListClient_OIDC.Models;
using Microsoft.AspNetCore.Authorization;

namespace ListClient_OIDC.Controllers
{
    [Authorize]
    public class ListController : Controller
    { 
        private IConfiguration _configuration;

        public ListController(IConfiguration config) {
            _configuration = config;
        }


        private async Task<AuthenticationResult> GetAccessTokenAsync() {
            string userObjectID = (User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;

            string authority = $"{_configuration["AzureAd:Instance"]}{_configuration["AzureAd:TenantId"]}";
            string client_id = _configuration["AzureAd:ClientId"];
            string client_secret = _configuration["AzureAd:ClientSecret"];
            string listapi_resource = _configuration["AzureAd:ListAPIResource"];
            
            AuthenticationContext authContext = new AuthenticationContext(authority, new NaiveSessionCache(userObjectID, HttpContext.Session));
            ClientCredential credential = new ClientCredential(client_id, client_secret);
            AuthenticationResult result = await authContext.AcquireTokenSilentAsync(listapi_resource, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            return result;
        }

        public async Task<IActionResult> Index()
        {
            AuthenticationResult auth = await GetAccessTokenAsync();
            string accessToken = auth.AccessToken;

            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://listapi.azurewebsites.net/api/list");

            var cont = await response.Content.ReadAsStringAsync();

             List<ListItem> items = new List<ListItem>();
            List<ListItem> returnItems = JsonConvert.DeserializeObject< List<ListItem> >(cont);
            if (returnItems != null) {
                items = returnItems;
            }

            return View(items);
        }

        public async Task<IActionResult> Delete(int id)
        {
            AuthenticationResult auth = await GetAccessTokenAsync();
            string accessToken = auth.AccessToken;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.DeleteAsync("https://listapi.azurewebsites.net/api/list/" + id.ToString());

            return RedirectToAction("Index");
        }

        
        public async Task<IActionResult> New(string description)
        {
            AuthenticationResult auth = await GetAccessTokenAsync();
            string accessToken = auth.AccessToken;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            ListItem item = new ListItem();
            item.Description = description;

            var stringContent = new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://listapi.azurewebsites.net/api/list/", stringContent);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ToggleComplete(int id)
        {
            AuthenticationResult auth = await GetAccessTokenAsync();
            string accessToken = auth.AccessToken;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync("https://listapi.azurewebsites.net/api/list/" + id.ToString());
            var cont = await response.Content.ReadAsStringAsync();
            ListItem item = JsonConvert.DeserializeObject<ListItem>(cont);

            //Toggle
            item.Completed = !item.Completed;

            var stringContent = new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");
            response = await client.PutAsync ("https://listapi.azurewebsites.net/api/list/" + id.ToString(), stringContent);

            return RedirectToAction("Index");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
