using IdentityModel;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.DependencyInjection
{
    static public class KnownUserDbPropertyInfos
    {
        static public DbPropertyInfo UserName(DbPropertyInfoAction action = DbPropertyInfoAction.ReadOnly)
        {
            return new DbPropertyInfo("UserName", "Username", typeof(string), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.PreferredUserName
            };
        }

        static public DbPropertyInfo GivenName(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        {
            return new DbPropertyInfo("GivenName", "Givenname", typeof(string), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.GivenName
            };
        }

        static public DbPropertyInfo FamilyName(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        {
            return new DbPropertyInfo("FamilyName", "Familyname", typeof(string), "Profile")
            {
                Action = action,
                ClaimName=JwtClaimTypes.FamilyName
            };
        }

        static public DbPropertyInfo Organisation(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        {
            return new DbPropertyInfo("Organisation", "Organisation", typeof(string), "Profile")
            {
                ClaimName = "organisation"
            };
        }

        static public DbPropertyInfo BirthDate(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        {
            return new DbPropertyInfo("BirthDay", "Birthday", typeof(DateTime), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.BirthDate
            };
        }

        static public DbPropertyInfo PhoneNumber(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        {
            return new DbPropertyInfo("PhoneNumber", "Phone number", typeof(string), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.PhoneNumber
            };
        }

        static public DbPropertyInfo Email(DbPropertyInfoAction action = DbPropertyInfoAction.ReadOnly | DbPropertyInfoAction.CanChange)
        {
            return new DbPropertyInfo("Email", "Email", typeof(string), "Email")
            {
                Action = action,
                ClaimName = JwtClaimTypes.Email
            };
        }

        static public DbPropertyInfo PasswordHash(DbPropertyInfoAction action = DbPropertyInfoAction.CanChange)
        {
            return new DbPropertyInfo("PasswordHash", "Password", typeof(string), "Password")
            {
                Action = action
            };
        }

        static public DbPropertyInfo TwoFactorEnabled(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        {
            return new DbPropertyInfo("TwoFactorEnabled", "TwoFactorEnabled", typeof(bool), "Two-factor authentication")
            {
                Action = action
            };
        }
    }
}
