using IdentityServerNET.Models.IdentityServerWrappers;
using System.Collections.Generic;

namespace IdentityServerNET.Abstractions.Services;

public class ClientDbContextConfiguration
{
    public IEnumerable<ClientModel>? IntialClients { get; set; }

    public string ConnectionString { get; set; } = "";
    public string TableName { get; set; } = "";
}
