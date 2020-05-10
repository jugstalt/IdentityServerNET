using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IdentityServer.Legacy.Extensions
{
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
    }
}