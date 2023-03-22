using IdentityServer.Legacy.Services.UI;
using Microsoft.Extensions.Options;

namespace IdentityServer.Legacy.ServerExtension.Test.Services.UI
{
    internal class TestUserInterfaceService : UserInterfaceService
    {
        public TestUserInterfaceService(IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor)
            : base(optionsMonitor)
        {
        }
    }
}
