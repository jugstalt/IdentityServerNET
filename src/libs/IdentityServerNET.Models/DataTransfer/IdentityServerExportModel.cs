using IdentityServerNET.Models.IdentityServerWrappers;
using System.Collections.Generic;

namespace IdentityServerNET.Models.DataTransfer;

public class IdentityServerExportModel
{
    public string SchemaVersion { get; set; } = "1.0";
    public string ExportedAt { get; set; } = "";
    public string SourceOrigin { get; set; } = "";

    public IEnumerable<ApplicationUser> Users { get; set; } = [];
    public IEnumerable<ApplicationRole> Roles { get; set; } = [];
    public IEnumerable<ClientModel> Clients { get; set; } = [];
    public IEnumerable<ApiResourceModel> ApiResources { get; set; } = [];
    public IEnumerable<IdentityResourceModel> IdentityResources { get; set; } = [];
}
