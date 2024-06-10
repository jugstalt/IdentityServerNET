using System;

namespace IdentityServer.Nova.Clients.Extensions
{
    static public class SecretsVaultExtensions
    {
        static public string SecretsVaultLockerName(this string path)
        {
            return path.Split('/')[0];
        }

        static public string SecretsValueSecretName(this string path)
        {
            var parts = path.Split('/');
            if (parts.Length < 2)
            {
                throw new ArgumentException($"Invalid secrets vault path {path}");
            }

            return parts[1];
        }

        static public long SecretsVaultSecretVersion(this string path)
        {
            var parts = path.Split('/');
            if (parts.Length < 2)
            {
                return 0;
            }

            if (!long.TryParse(parts[2], out long version))
            {
                throw new ArgumentException($"Invalid secrets vault path {path}");
            }

            return version;
        }
    }
}
