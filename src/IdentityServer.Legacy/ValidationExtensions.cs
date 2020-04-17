using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace IdentityServer.Legacy
{
    public static class ValidationExtensions
    {
        // ^(?=.{8,20}$)(?![_.])(?!.*[_.]{2})[a-zA-Z0-9._]+(?<![_.])$
        //  └─────┬────┘└───┬──┘└─────┬─────┘└─────┬─────┘ └───┬───┘
        //        │         │         │            │           no _ or.at the end
        //        │         │         │            │
        //        │         │         │            allowed characters
        //        │         │         │
        //        │         │         no __ or _.or._ or .. inside
        //        │         │
        //        │         no _ or.at the beginning
        //        │
        //        username is 8-20 characters long
        public const string GeneralUsernameRegex = "^(?=.{8,20}$)(?![_-])(?!.*[_-]{2})[a-zA-Z0-9-_]+(?<![_-])$";
        public const string EmailAddressRegex =
             @"^([0-9a-zA-Z]" + //Start with a digit or alphabate
                @"([\+\-_\.][0-9a-zA-Z]+)*" + // No continues or ending +-_. chars in email
                @")+" +
                @"@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17})$";
        public const string PhoneNumberRegex = @"^[+]*[(]{0,1}[0-9]{1,4}[)]{0,1}[-\s\./0-9]*$";
        public const string PromotionCodeRegex = @"^[a-z0-9]*$";
        public const string NamespaceRegex = @"^[a-z0-9\-]*$";
        public const string NumberRegex = @"^[0-9]*$";
        public const string DecimalRegex = @"^[0-9](\.[0-9]+)?$";

        public static bool IsValidGeneralUsername(this string username)
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            return username.CheckRegex(GeneralUsernameRegex);
        }

        public static bool IsValidEmailAddress(this string email)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return email.CheckRegex(EmailAddressRegex);
        }

        public static bool IsValidPromotionCode(this string promotionCode)
        {
            if (String.IsNullOrWhiteSpace(promotionCode) || promotionCode.Length < 8)
            {
                return false;
            }

            return promotionCode.CheckRegex(PromotionCodeRegex);
        }

        public static bool IsValidNamespace(this string @namespace)
        {
            if (String.IsNullOrWhiteSpace(@namespace) || @namespace.Length < 3)
            {
                return false;
            }

            if (@namespace.StartsWith("-") ||
                @namespace.EndsWith("-") ||
                @namespace.Contains("--"))
            {
                return false;
            }

            return @namespace.CheckRegex(NamespaceRegex);
        }

        
        public static bool CheckRegex(this string input, string regex)
        {
            if (String.IsNullOrWhiteSpace(regex))
                return true;

            return new Regex(regex).IsMatch(input);
        }
    }
}
