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
                string[]? apiScopes,
                string[]? additionalRedirectUris = null,
                string[]? additionalGrantTypes = null)
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
                    if (apiScopes.Contains("offline_access"))
                    {
                        client.AllowOfflineAccess = true;
                    }
                }

                if (!String.IsNullOrWhiteSpace(clientUrl))
                {
                    try
                    {
                        var baseUri = new Uri(clientUrl);
                        var redirectUris = new List<string>
                        {
                            new Uri(baseUri, "signin-oidc").ToString()
                        };
                        if (additionalRedirectUris?.Length > 0)
                        {
                            redirectUris.AddRange(
                                additionalRedirectUris.Select(r => new Uri(baseUri, r).ToString()));
                        }
                        client.RedirectUris = redirectUris;
                        client.PostLogoutRedirectUris = new[]
                        {
                            new Uri(baseUri, "signout-callback-oidc").ToString()
                        };
                    }
                    catch { }
                }

                if (additionalGrantTypes?.Length > 0)
                {
                    var grantTypes = client.AllowedGrantTypes?.ToList() ?? [];
                    grantTypes.AddRange(additionalGrantTypes);
                    client.AllowedGrantTypes = grantTypes;
                }

                break;
        }
    }
}
