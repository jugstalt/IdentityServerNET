using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace IdentityServer.Nova.Extensions;
static public class ConfigurationExtensions
{
    static public string StorageRootPath(this IConfiguration configuration)
        =>
        configuration is not null && !String.IsNullOrEmpty(configuration["IdentityServer:StorageRootPath"])
        ? configuration["IdentityServer:StorageRootPath"]
        : (SystemInfo.IsLinux, SystemInfo.IsWindows, SystemInfo.IsOSX) switch
        {
            (true, false, false) => "/home/app/identityserver-nova",
            (false, true, false) => "C:\\apps\\identityserver-nova",
            (false, false, true) => "/home/app/identityserver-nova",
            _ => throw new System.Exception("Unknown plattform!")
        };

    static public string StoragePath(this IConfiguration configuration)
        => Path.Combine(configuration.StorageRootPath(), "storage");

    static public string SecretsVaultPath(this IConfiguration configuration)
        => Path.Combine(configuration.StoragePath(), "secretsvault");

    static public string ValidationCertsPath(this IConfiguration configuration)
        => Path.Combine(configuration.StoragePath(), "validation");

    static public string AssetPath(this IConfiguration configuration, string path)
    {
        path = path.RemoveTildePath();

        return path.IsAbsoluteOrRelativePath()
           ? path
           : Path.Combine(configuration.StorageRootPath(), path ?? "");
    }

    static public string StorageAssetPath(this IConfiguration configuration, string path)
    {
        path = path.RemoveTildePath();

        return path.IsAbsoluteOrRelativePath()
           ? path
           : Path.Combine(configuration.StoragePath(), path ?? "");
    }

    #region Helper

    static private bool IsAbsoluteOrRelativePath(this string path) 
        => 
        String.IsNullOrEmpty(path) 
        ? false
        : (SystemInfo.IsLinux, SystemInfo.IsWindows, SystemInfo.IsOSX) switch
        {
            (true, false, false) => path.StartsWith("/") || path.StartsWith("./"),
            (false, true, false) => path.Contains(":") || path.StartsWith("."),
            (false, false, true) => path.StartsWith("/") || path.StartsWith("./"),
            _ => throw new System.Exception("Unknown plattform!")
        };

    static private string RemoveTildePath(this string path) => path switch
    {
        null => String.Empty,
        { Length: > 1 } when path.StartsWith("~/") || path.StartsWith(@"~\") => path.Substring(2),
        { Length: > 0 } when path.StartsWith("~") => path.Substring(1),
        _ => path
    };

    #endregion
}

