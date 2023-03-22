using System;
using System.Globalization;
using System.Linq;

namespace IdentityServer.Legacy.Extensions
{
    static public class PropertyExtensions
    {
        static public string ToClaimString(this string str)
        {
            return str ?? String.Empty;
        }

        static public string ToClaimArrayString(this object[] array)
        {
            if (array == null)
            {
                return String.Empty;
            }

            var stringArray = array.Select(o => o?.ToString()).ToArray();

            return String.Join("\n", stringArray);
        }

        static public string[] ToPropertyValueArray(this string propertyValue)
        {
            if (String.IsNullOrWhiteSpace(propertyValue))
            {
                return null;
            }

            return propertyValue
                        .Replace("\r", "")
                        .Split('\n')
                        .Select(s => s.Trim())
                        .Where(s => !String.IsNullOrEmpty(s))
                        .ToArray();
        }

        static public bool IsPropertySequenceEqual(this string[] array, object[] candidate)
        {
            if (array == null && candidate == null)
            {
                return true;
            }

            if (array == null && candidate != null)
            {
                return false;
            }

            if (array != null && candidate == null)
            {
                return false;
            }

            if (array.Length != candidate.Length)
            {
                return false;
            }

            return array.SequenceEqual(candidate.Select(o => o?.ToString()));
        }

        #region Numbers

        static public double ToPlatformDouble(this string value)
        {
            if (SystemInfo.IsWindows)
            {
                return double.Parse(value.Replace(",", "."), SystemInfo.Nhi);
            }

            return double.Parse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator));
        }

        static public float ToPlatformFloat(this string value)
        {
            if (SystemInfo.IsWindows)
            {
                return float.Parse(value.Replace(",", "."), SystemInfo.Nhi);
            }

            return float.Parse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator));
        }

        static public bool TryToPlatformDouble(this string value, out double result)
        {
            if (SystemInfo.IsWindows)
            {
                return double.TryParse(value.Replace(",", "."), NumberStyles.Any, SystemInfo.Nhi, out result);
            }

            return double.TryParse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator), out result);
        }

        static public bool TryToPlatformFloat(this string value, out float result)
        {
            if (SystemInfo.IsWindows)
            {
                return float.TryParse(value.Replace(",", "."), NumberStyles.Any, SystemInfo.Nhi, out result);
            }

            return float.TryParse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator), out result);
        }

        static public string ToPlatformNumberString(this double value)
        {
            return value.ToString(SystemInfo.Nhi);
        }

        #endregion
    }
}
