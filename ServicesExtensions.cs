using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureManage
{
    
    public static class ServicesExtensions{
        public static IServiceCollection AddMicrosoftIdentityPlatformAuthentication(
                this IServiceCollection services,
                IConfiguration configuration,
                string configSectionName = "AzureAd",
                bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false)
            {

                services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                    .AddAzureAD(options => configuration.Bind(configSectionName, options));
                services.Configure<AzureADOptions>(options => configuration.Bind(configSectionName, options));

                services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
                {
                    // Per the code below, this application signs in users in any Work and School
                    // accounts and any Microsoft Personal Accounts.
                    // If you want to direct Azure AD to restrict the users that can sign-in, change
                    // the tenant value of the appsettings.json file in the following way:
                    // - only Work and School accounts => 'organizations'
                    // - only Microsoft Personal accounts => 'consumers'
                    // - Work and School and Personal accounts => 'common'
                    // If you want to restrict the users that can sign-in to only one tenant
                    // set the tenant value in the appsettings.json file to the tenant ID
                    // or domain of this organization
                    options.Authority = options.Authority + "/v2.0/";

                    // If you want to restrict the users that can sign-in to several organizations
                    // Set the tenant value in the appsettings.json file to 'organizations', and add the
                    // issuers you want to accept to options.TokenValidationParameters.ValidIssuers collection
                    options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetIssuerValidator(options.Authority).Validate;

                    // Set the nameClaimType to be preferred_username.
                    // This change is needed because certain token claims from Azure AD V1 endpoint
                    // (on which the original .NET core template is based) are different than Microsoft identity platform endpoint.
                    // For more details see [ID Tokens](https://docs.microsoft.com/azure/active-directory/develop/id-tokens)
                    // and [Access Tokens](https://docs.microsoft.com/azure/active-directory/develop/access-tokens)
                    options.TokenValidationParameters.NameClaimType = "preferred_username";
                    
                    // Avoids having users being presented the select account dialog when they are already signed-in
                    // for instance when going through incremental consent
                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        var login = context.Properties.GetParameter<string>(OpenIdConnectParameterNames.LoginHint);
                        if (!string.IsNullOrWhiteSpace(login))
                        {
                            context.ProtocolMessage.LoginHint = login;
                            context.ProtocolMessage.DomainHint = context.Properties.GetParameter<string>(
                                OpenIdConnectParameterNames.DomainHint);

                            // delete the login_hint and domainHint from the Properties when we are done otherwise
                            // it will take up extra space in the cookie.
                            context.Properties.Parameters.Remove(OpenIdConnectParameterNames.LoginHint);
                            context.Properties.Parameters.Remove(OpenIdConnectParameterNames.DomainHint);
                        }

                        // Additional claims
                        if (context.Properties.Items.ContainsKey(OidcConstants.AdditionalClaims))
                        {
                            context.ProtocolMessage.SetParameter(
                                OidcConstants.AdditionalClaims,
                                context.Properties.Items[OidcConstants.AdditionalClaims]);
                        }

                        return Task.FromResult(0);
                    };
                });
                return services;
            }
    }
}
