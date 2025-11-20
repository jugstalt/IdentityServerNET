#nullable enable

using IdentityServer4.Models;
using IdentityServerNET.Models.IdentityServerWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using static IdentityServer.Areas.Admin.Pages.Clients.ClientsModel;

namespace IdentityServerNET.Extensions;

internal static class ClientModelExtensions
{
    static public void ApplyTemplate(
                this ClientModel client,
                ClientTemplateType templateType,
                string? clientUrl,
                string[]? apiScopes)
    {
        switch (templateType)
        {
            case ClientTemplateType.ApiClient:
                client.AllowedGrantTypes = GrantTypes.ClientCredentials;
                client.RequireClientSecret = true;
                client.RequireConsent = client.AllowRememberConsent = false;
                if (apiScopes is not null)
                {
                    client.AllowedScopes = apiScopes.ToList();
                }
                break;
            case ClientTemplateType.JavascriptClient:
                client.AllowedGrantTypes = GrantTypes.Code;
                client.RequirePkce = true;
                client.RequireClientSecret = false;

                if (!String.IsNullOrWhiteSpace(clientUrl))
                {
                    client.RedirectUris = new[] { clientUrl + "/callback.html" };
                    client.PostLogoutRedirectUris = new[] { clientUrl + "/index.html" };
                    client.AllowedCorsOrigins = new[] { clientUrl };
                };

                client.AllowedScopes = new HashSet<string>()
                {
                    IdentityServer4.IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServer4.IdentityServerConstants.StandardScopes.Profile
                };
                if (apiScopes?.Any() == true)
                {
                    foreach(var apiScope in apiScopes)
                    {
                       client.AllowedScopes.Add(apiScope);
                    }
                }
                break;
            case ClientTemplateType.WebApplication:
                client.AllowedGrantTypes = GrantTypes.Code;
                client.RequireClientSecret = true;
                client.RequireConsent = true;
                client.AllowRememberConsent = true;
                client.RequirePkce = true;
                client.AlwaysIncludeUserClaimsInIdToken = true;

                client.AllowedScopes = new HashSet<string>
                {
                    IdentityServer4.IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServer4.IdentityServerConstants.StandardScopes.Profile
                };
                if (apiScopes?.Any() == true)
                {
                    foreach(var apiScope in apiScopes)
                    {
                       client.AllowedScopes.Add(apiScope);
                    }
                }

                if (!String.IsNullOrWhiteSpace(clientUrl))
                {
                    try
                    {
                        client.RedirectUris = new[]
                        {
                            clientUrl = new Uri(new Uri(clientUrl), "signin-oidc").ToString()
                        };
                        client.PostLogoutRedirectUris = new[]
                        {
                            clientUrl = new Uri(new Uri(clientUrl), "signout-callback-oidc").ToString()
                        };
                    }
                    catch { }
                }


                break;
        }
    }
}
