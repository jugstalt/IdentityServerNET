using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.DbContext
{
    public static class ApplicationUserProperties
    {
        public const string NormalizedUserName = "NormalizedUserName";
        public const string UserName = "UserName";
        public const string Email = "Email";
        public const string EmailConfirmed = "EmailConfirmed";
        public const string NormalizedEmail = "NormalizedEmail";
        public const string PasswordHash = "PasswordHash";
        public const string PhoneNumber = "PhoneNumber";
        public const string PhoneNumberConfirmed = "PhoneNumberConfirmed";
        public const string AuthenticatorKey = "AuthenticatorKey";
        public const string TwoFactorEnabled = "TwoFactorEnabled";
        public const string TfaRecoveryCodes = "TfaRecoveryCodes";
        public const string SecurityStamp = "SecurityStamp";
    }
}
