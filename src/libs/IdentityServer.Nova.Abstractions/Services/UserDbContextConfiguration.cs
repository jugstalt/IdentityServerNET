using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Abstractions.Serialize;
using IdentityServer.Nova.Models.UserInteraction;

namespace IdentityServer.Nova.Abstractions.Services;

public class UserDbContextConfiguration
{
    public string ConnectionString { get; set; }
    public ICryptoService CryptoService { get; set; }
    public IBlobSerializer BlobSerializer { get; set; }

    public ManageAccountEditor ManageAccountEditor { get; set; }
    public AdminAccountEditor AdminAccountEditor { get; set; }
    public RegisterAccountEditor RegisterAccountEditor { get; set; }
}
