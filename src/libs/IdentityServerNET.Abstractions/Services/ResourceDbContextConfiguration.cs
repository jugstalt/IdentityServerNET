using IdentityServerNET.Models.IdentityServerWrappers;
using System.Collections.Generic;

namespace IdentityServerNET.Abstractions.Services;

public class ResourceDbContextConfiguration
{
    public IEnumerable<ApiResourceModel>? InitialApiResources { get; set; }
    public IEnumerable<IdentityResourceModel>? InitialIdentityResources { get; set; }

    public string ConnectionString { get; set; } = "";
    public string TableName { get; set; } = "";
}
