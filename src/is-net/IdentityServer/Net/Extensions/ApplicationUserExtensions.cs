using Duende.IdentityModel;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace IdentityServerNET.Extensions;

static public class ApplicationUserExtensions
{
    static public string ApplicationUserName(this ApplicationUser user)
    {
        if (user == null)
        {
            return String.Empty;
        }

        if (user.Claims != null)
        {
            string name = $"{user.Claims.Where(c => c.Type == JwtClaimTypes.GivenName).FirstOrDefault()?.Value} {user.Claims.Where(c => c.Type == JwtClaimTypes.MiddleName).FirstOrDefault()?.Value} {user.Claims.Where(c => c.Type == JwtClaimTypes.FamilyName).FirstOrDefault()?.Value}";
            if (!String.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return user.UserName;
    }


    static public void SetAdministratorUserName(IWebHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment() && !String.IsNullOrWhiteSpace(configuration["IdentityServer:AdminUsername"]))
        {
            AdminUserName = configuration["IdentityServer:Admin:AdminUsername"];
        }
        else
        {
            AdminUserName = null;
        }
    }

    static private string AdminUserName { get; set; }

    static public bool IsUserAdministrator(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        // Realm-aware: matches the global role (system admin) or role@realm (realm admin).
        return user.Roles.Any(r => r.GetRealmScopedName() == KnownRoles.UserAdministrator);
    }

    static public bool IsRoleAdministrator(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        return user.Roles.Any(r => r.GetRealmScopedName() == KnownRoles.RoleAdministrator);
    }

    static public bool IsResourceAdministrator(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        return user.Roles.Any(r => r.GetRealmScopedName() == KnownRoles.ResourceAdministrator);
    }

    static public bool IsClientAdministrator(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        return user.Roles.Any(r => r.GetRealmScopedName() == KnownRoles.ClientAdministrator);
    }

    static public bool IsSecretVaultAdministrator(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        return user.Roles.Contains(KnownRoles.SecretsVaultAdministrator);
    }

    static public bool IsSignungUIAdministrator(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        return user.Roles.Contains(KnownRoles.SigningAdministrator);
    }

    // Realm administration is a system-level capability and is never realm-scoped.
    static public bool IsRealmAdministrator(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        return user.Roles.Contains(KnownRoles.RealmAdministrator);
    }

    static public bool HasAdministratorRole(this ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
        {
            return true;
        }

        if (user?.Roles == null)
        {
            return false;
        }

        return user.IsUserAdministrator() ||
               user.IsRoleAdministrator() ||
               user.IsResourceAdministrator() ||
               user.IsClientAdministrator() ||
               user.IsSecretVaultAdministrator() ||
               user.IsSignungUIAdministrator() ||
               user.IsRealmAdministrator();
    }
}
