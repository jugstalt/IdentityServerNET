using IdentityModel;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.DbContext
{
    static public class KnownUserDbPropertyInfos
    {
        static public DbPropertyInfo ReadOnlyUserName(DbPropertyInfoAction action = DbPropertyInfoAction.ReadOnly)
        {
            return new DbPropertyInfo("UserName", "Username", typeof(string), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.PreferredUserName,
                IsRequired=true
            };
        }

        static public DbPropertyInfo EditableUserName(DbPropertyInfoAction action = DbPropertyInfoAction.ReadOnly)
        {
            return new DbPropertyInfo("UserName", "Username", typeof(string), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.PreferredUserName,
                RegexPattern = ValidationExtensions.GeneralUsernameRegex,
                IsRequired = true
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
                ClaimName = JwtClaimTypes.BirthDate,

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

        static public DbPropertyInfo EditableEmail(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        {
            return new DbPropertyInfo("Email", "Email", typeof(string), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.Email,
                RegexPattern = ValidationExtensions.EmailAddressRegex
            };
        }

        static public DbPropertyInfo ReadOnlyEmail(DbPropertyInfoAction action = DbPropertyInfoAction.ReadOnly)
        {
            return new DbPropertyInfo("Email", "Email", typeof(string), "Profile")
            {
                Action = action,
                ClaimName = JwtClaimTypes.Email
            };
        }

        //static public DbPropertyInfo PasswordHash(DbPropertyInfoAction action = DbPropertyInfoAction.CanChange)
        //{
        //    return new DbPropertyInfo("PasswordHash", "Password", typeof(string), "Password")
        //    {
        //        Action = action
        //    };
        //}

        //static public DbPropertyInfo TwoFactorEnabled(DbPropertyInfoAction action = DbPropertyInfoAction.Editable)
        //{
        //    return new DbPropertyInfo("TwoFactorEnabled", "TwoFactorEnabled", typeof(bool), "Two-factor authentication")
        //    {
        //        Action = action
        //    };
        //}
    }
}
