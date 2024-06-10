using IdentityServer.Nova.Services.UI;
using Microsoft.Extensions.Options;

namespace IdentityServer.Nova.ServerExtension.Test.Services.UI
{
    internal class TestUserInterfaceService : UserInterfaceService
    {
        public TestUserInterfaceService(IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor)
            : base(optionsMonitor)
        {
        }
    }
}
