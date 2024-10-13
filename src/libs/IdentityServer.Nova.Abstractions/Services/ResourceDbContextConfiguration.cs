using IdentityServer.Nova.Models.IdentityServerWrappers;
using System.Collections.Generic;

namespace IdentityServer.Nova.Abstractions.Services;

public class ResourceDbContextConfiguration
{
    public IEnumerable<ApiResourceModel>? InitialApiResources { get; set; }
    public IEnumerable<IdentityResourceModel>? InitialIdentityResources { get; set; }

    public string ConnectionString { get; set; } = "";
    public string TableName { get; set; } = "";
}
