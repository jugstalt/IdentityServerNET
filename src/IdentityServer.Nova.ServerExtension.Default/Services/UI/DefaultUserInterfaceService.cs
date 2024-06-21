using IdentityServer.Nova.Services.UI;
using Microsoft.Extensions.Options;

namespace IdentityServer.Nova.ServerExtension.Default.Services.UI
{
    internal class DefaultUserInterfaceService : UserInterfaceService
    {
        public DefaultUserInterfaceService(IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor)
            : base(optionsMonitor)
        {
        }
    }
}
