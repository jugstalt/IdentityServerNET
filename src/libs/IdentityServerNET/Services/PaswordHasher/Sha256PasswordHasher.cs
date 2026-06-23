#nullable enable

using Duende.IdentityModel;
using IdentityServerNET.Models;
using Microsoft.Extensions.Options;

namespace IdentityServerNET.Services.PasswordHasher;

public class Sha256PasswordHasher : PasswordHasher
{
    private readonly string _template;

    public Sha256PasswordHasher(IOptions<PasswordHashingOptions>? hashingOptions = null)
    {
        _template = hashingOptions?.Value?.Template ?? "{password}";
    }

    public override string HashPassword(ApplicationUser user, string password)
        => _template.ApplyPasswordHashingTemplate(user, password).ToSha256();
}
