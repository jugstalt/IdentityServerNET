#nullable enable

namespace IdentityServer.Nova.Models;

public class MigrationModel
{
    public IdentityResouce[]? IdentityResouces { get; set; }

    public ApiResource[]? ApiResources { get; set; }   

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

    #endregion
}
