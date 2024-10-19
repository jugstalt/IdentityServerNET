#nullable enable

namespace IdentityServer.Nova.Models;

public class MigrationModel
{
    public string? AdminPassword { get; set; } = null;

    public IdentityResource[]? IdentityResources { get; set; }

    public ApiResource[]? ApiResources { get; set; }

    public Role[]? Roles { get; set; }

    public User[]? Users { get; set; }

    public Client[]? Clients { get; set; }

    #region

    public record IdentityResource(string Name);

    public class ApiResource()
    {
        public string Name { get; set; } = "";
        public Scope[]? Scopes { get; set; }
    }

    public class Scope()
    {
        public string Name { get; set; } = "";
    }

    public record Role(string Name);

    public class User()
    {
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
        public string[]? Roles { get; set; }
    }

    public class Client()
    {
        public string ClientType { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string? ClientUrl { get; set; }
        public string[]? Scopes { get; set; }
    }

    #endregion
}
