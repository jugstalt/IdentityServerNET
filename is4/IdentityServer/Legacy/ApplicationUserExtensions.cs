using IdentityModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy
{
    static public class ApplicationUserExtensions
    {
        static public string ApplicationUserName(this ApplicationUser user)
        {
            if (user == null)
                return String.Empty;

            if(user.Claims!=null)
            {
                string name = $"{ user.Claims.Where(c => c.Type == JwtClaimTypes.GivenName).FirstOrDefault()?.Value } { user.Claims.Where(c => c.Type == JwtClaimTypes.MiddleName).FirstOrDefault()?.Value} { user.Claims.Where(c => c.Type == JwtClaimTypes.FamilyName).FirstOrDefault()?.Value}";
                if(!String.IsNullOrWhiteSpace(name))
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
                AdminUserName = configuration["IdentityServer:AdminUsername"];
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
                return true;

            if (user?.Roles == null)
                return false;

            return user.Roles.Contains(KnownRoles.UserAdministrator);
        }

        static public bool IsRoleAdministrator(this ApplicationUser user)
        {
            if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
                return true;

            if (user?.Roles == null)
                return false;

            return user.Roles.Contains(KnownRoles.RoleAdministrator);
        }

        static public bool IsResourceAdministrator(this ApplicationUser user)
        {
            if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
                return true;

            if (user?.Roles == null)
                return false;

            return user.Roles.Contains(KnownRoles.ResourceAdministrator);
        }

        static public bool IsClientAdministrator(this ApplicationUser user)
        {
            if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
                return true;

            if (user?.Roles == null)
                return false;

            return user.Roles.Contains(KnownRoles.ClientAdministrator);
        }

        static public bool HasAdministratorRole(this ApplicationUser user)
        {
            if (!String.IsNullOrWhiteSpace(AdminUserName) && AdminUserName.Equals(user?.UserName))
                return true;

            if (user?.Roles == null)
                return false;

            return user.IsUserAdministrator() || user.IsRoleAdministrator() || user.IsResourceAdministrator() || user.IsClientAdministrator();
        }
    }
}
