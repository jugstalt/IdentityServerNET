using IdentityModel;
using IdentityServer.Nova.Models.Extensions;
using System;

namespace IdentityServer.Nova.Models.UserInteraction;

static public class KnownUserEditorInfos
{
    static public EditorInfo ReadOnlyUserName(EditorType action = EditorType.ReadOnly)
    {
        return new EditorInfo("UserName", "Username", typeof(string), "Profile")
        {
            EditorType = action,
            ClaimName = JwtClaimTypes.PreferredUserName,
        };
    }

    static public EditorInfo EditableUserName(EditorType action = EditorType.ReadOnly | EditorType.Required)
    {
        return new EditorInfo("UserName", "Username", typeof(string), "Profile")
        {
            EditorType = action,
            ClaimName = JwtClaimTypes.PreferredUserName,
            RegexPattern = ValidationExtensions.GeneralUsernameRegex,
        };
    }

    static public EditorInfo GivenName(EditorType action = EditorType.Editable)
    {
        return new EditorInfo("GivenName", "Givenname", typeof(string), "Profile")
        {
            EditorType = action,
            ClaimName = JwtClaimTypes.GivenName
        };
    }

    static public EditorInfo FamilyName(EditorType action = EditorType.Editable)
    {
        return new EditorInfo("FamilyName", "Familyname", typeof(string), "Profile")
        {
            EditorType = action,
            ClaimName = JwtClaimTypes.FamilyName
        };
    }

    static public EditorInfo Organisation(EditorType action = EditorType.Editable)
    {
        return new EditorInfo("Organisation", "Organisation", typeof(string), "Profile")
        {
            ClaimName = "organisation"
        };
    }

    static public EditorInfo BirthDate(EditorType action = EditorType.Editable | EditorType.Date)
    {
        return new EditorInfo("BirthDay", "Birthday", typeof(DateTime), "Profile")
        {
            EditorType = action,
            ClaimName = JwtClaimTypes.BirthDate,

        };
    }

    static public EditorInfo PhoneNumber(EditorType action = EditorType.Editable | EditorType.Phone /*| DbPropertyInfoEditorType.Required*/)
    {
        return new EditorInfo("PhoneNumber", "Phone number", typeof(string), "Profile")
        {
            EditorType = action,
            ClaimName = JwtClaimTypes.PhoneNumber
        };
    }

    static public EditorInfo EditableEmail(EditorType action = EditorType.Editable | EditorType.EmailAddress)
    {
        return new EditorInfo("Email", "Email", typeof(string), "Profile")
        {
            EditorType = action,
            ClaimName = JwtClaimTypes.Email,
            RegexPattern = ValidationExtensions.EmailAddressRegex
        };
    }

    static public EditorInfo ReadOnlyEmail(EditorType action = EditorType.ReadOnly)
    {
        return new EditorInfo("Email", "Email", typeof(string), "Profile")
        {
            EditorType = action,
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
