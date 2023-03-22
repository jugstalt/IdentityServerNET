using System.Globalization;
using System.Runtime.InteropServices;

namespace IdentityServer.Legacy.Extensions
{
    public class SystemInfo
    {
        static public NumberFormatInfo Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        static public NumberFormatInfo Cnf = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;

        static public bool IsLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        static public bool IsWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        static public bool IsOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        static public string Platform
        {
            get
            {
                if (IsLinux)
                {
                    return OSPlatform.Linux.ToString();
                }

                if (IsOSX)
                {
                    return OSPlatform.OSX.ToString();
                }

                if (IsWindows)
                {
                    return OSPlatform.Windows.ToString();
                }

                return "Unknown";
            }
        }
    }
}
