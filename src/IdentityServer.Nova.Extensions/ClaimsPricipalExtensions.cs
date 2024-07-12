using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace IdentityServer.Nova.Extensions;

static public class ClaimsPricipalExtensions
{
    static public bool IsInOpenIdConnectRole(this ClaimsPrincipal claimsPrincipal, string role)
    {
        return claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == role)
                    .FirstOrDefault() != null;
    }

    static public IEnumerable<string> GetRoles(this ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Claims == null)
        {
            return new string[0];
        }

        return claimsPrincipal
                .Claims
                .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value);
    }

    static public string GetEmail(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                            .Claims?
                            .Where(c => c.Type == "email" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                            .FirstOrDefault()?
                            .Value;
    }

    static public string GetUsername(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.Identity.Name;
    }

    static public string GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" || c.Type == "sub")
                    .FirstOrDefault()?
                    .Value;
    }

    static public string[] GetScopes(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == "scope")
                    .Select(c => c.Value)
                    .ToArray();
    }

    static public string GetClientId(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == "client_id")
                    .FirstOrDefault()?
                    .Value;
    }

    static public string GetUsernameOrClientId(this ClaimsPrincipal claimsPrincipal)
    {
        var username = claimsPrincipal.GetUsername();

        if (!String.IsNullOrEmpty(username))
        {
            return username;
        }

        return claimsPrincipal.GetClientId();
    }
}
