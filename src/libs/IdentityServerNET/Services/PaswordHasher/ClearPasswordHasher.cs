#nullable enable

using IdentityServerNET.Models;
using Microsoft.Extensions.Options;

namespace IdentityServerNET.Services.PasswordHasher;

public class ClearPasswordHasher : PasswordHasher
{
    private readonly string _template;

    public ClearPasswordHasher(IOptions<PasswordHashingOptions>? hashingOptions = null)
    {
        _template = hashingOptions?.Value?.Template ?? "{password}";
    }

    public override string HashPassword(ApplicationUser user, string password)
        => _template.ApplyPasswordHashingTemplate(user, password);
}
