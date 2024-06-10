using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.Serialize;
using IdentityServer.Nova.UserInteraction;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    public class UserDbContextConfiguration
    {
        public string ConnectionString { get; set; }
        public ICryptoService CryptoService { get; set; }
        public IBlobSerializer BlobSerializer { get; set; }

        public ManageAccountEditor ManageAccountEditor { get; set; }
        public AdminAccountEditor AdminAccountEditor { get; set; }
        public RegisterAccountEditor RegisterAccountEditor { get; set; }
    }
}
