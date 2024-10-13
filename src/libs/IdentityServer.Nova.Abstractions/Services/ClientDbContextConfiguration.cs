using IdentityServer.Nova.Models.IdentityServerWrappers;
using System.Collections.Generic;

namespace IdentityServer.Nova.Abstractions.Services;

public class ClientDbContextConfiguration
{
    public IEnumerable<ClientModel>? IntialClients { get; set; }

    public string ConnectionString { get; set; } = "";
    public string TableName { get; set; } = "";
}
