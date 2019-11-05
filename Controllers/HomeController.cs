using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureManage.Models;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;

namespace AzureManage.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            this.config = config;
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Index()
        {
            //Grab the Bearer token from the HTTP Header using the identity bootstrap context. This requires SaveSigninToken to be true at Startup.Auth.cs
            var bootstrapContext = this.HttpContext.User.Identities.First().BootstrapContext.ToString();

            // Creating a UserAssertion based on the Bearer token sent by TodoListClient request.
            //urn:ietf:params:oauth:grant-type:jwt-bearer is the grant_type required when using On Behalf Of flow: https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow
            var userAssertion = new UserAssertion(bootstrapContext, "urn:ietf:params:oauth:grant-type:jwt-bearer");

            // Creating a ConfidentialClientApplication using the Build pattern (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-Applications)
            var app = ConfidentialClientApplicationBuilder.Create(config["AzureAd:ClientId"])
               .WithAuthority(config["AzureAd:Instance"])
               .WithClientSecret(config["AzureAd:ClientSecret"])
               .WithRedirectUri($"{this.Request.Scheme}://{this.Request.Host}{config["AzureAd:CallbackPath"]}")
               .Build();

            // Acquiring an AuthenticationResult for the scope user.read, impersonating the user represented by userAssertion, using the OBO flow
            var result = await app.AcquireTokenOnBehalfOf(new string[] { "https://management.azure.com/user_impersonation" }, userAssertion)
                .ExecuteAsync();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}