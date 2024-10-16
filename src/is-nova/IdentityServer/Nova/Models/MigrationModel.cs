#nullable enable

namespace IdentityServer.Nova.Models;

public class MigrationModel
{
    public string? AdminPassword { get; set; } = null;

    public IdentityResouce[]? IdentityResouces { get; set; }

    public ApiResource[]? ApiResources { get; set; } 
    
    public Role[]? Roles { get; set; }

    public User[]? Users { get; set; }

    #region

    public record IdentityResouce(string Name);

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

    #endregion
}
