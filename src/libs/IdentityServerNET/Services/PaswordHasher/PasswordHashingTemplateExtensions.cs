using IdentityServerNET.Models;
using System;

namespace IdentityServerNET.Services.PasswordHasher;

internal static class PasswordHashingTemplateExtensions
{
    internal static string ApplyPasswordHashingTemplate(this string template, ApplicationUser user, string password)
    {
        if (!template.Contains("{password}"))
            throw new InvalidOperationException(
                $"Password hashing template \"{template}\" does not contain the {{password}} placeholder. " +
                "Hashing without the actual password is a misconfiguration.");

        return template
            .Replace("{password}", password)
            .Replace("{email}",    user.Email?.ToLowerInvariant()    ?? "")
            .Replace("{username}", user.UserName?.ToLowerInvariant() ?? "");
    }
}
