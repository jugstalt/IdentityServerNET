using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace IdentityServer.Nova.Extensions;

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

    static public string DefaultWorkingPath()
        => (IsLinux, IsWindows, IsOSX) switch
        {
            (true, false, false) => "/etc/identityserver-nova",
            (false, true, false) => "C:\\apps\\identityserver-nova",
            (false, false, true) => "/etc/identityserver-nova",
            _ => throw new System.Exception("Unknown plattform!")
        };

    static public string DefaultStoragePath()
        => Path.Combine(DefaultWorkingPath(), "storage");
}
