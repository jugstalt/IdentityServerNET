using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;

namespace IdentityServerNET.Extensions;

static public class ClaimExtensions
{
    static public string ToClaimDateValue(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd"); // HH:mm
    }

    static public DateTime FromClaimDateValue(this string dateString)
    {
        return DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    static public bool TryAddClaim(this List<Claim> claims, string type, string value)
    {
        if (!String.IsNullOrEmpty(value))
        {
            try
            {
                claims.Add(new Claim(type, value));
                return true;
            }
            catch { }
        }

        return false;
    }
}