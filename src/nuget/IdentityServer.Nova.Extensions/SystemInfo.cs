using System.Globalization;
using System.Runtime.InteropServices;

namespace IdentityServer.Nova.Extensions;

public class SystemInfo
{
    static public NumberFormatInfo Nhi = CultureInfo.InvariantCulture.NumberFormat;
    static public NumberFormatInfo Cnf = CultureInfo.CurrentCulture.NumberFormat;

    static public bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    static public bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    static public bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    static public string Platform
        => (IsLinux, IsWindows, IsOSX) switch
        {
            (true, false, false) => OSPlatform.Linux.ToString(),
            (false, true, false) => OSPlatform.Windows.ToString(),
            (false, false, true) => OSPlatform.OSX.ToString(),
            _ => "Unknown platform"
        };
}
