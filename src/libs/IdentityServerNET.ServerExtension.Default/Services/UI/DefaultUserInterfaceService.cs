using IdentityServerNET.Services.UI;
using Microsoft.Extensions.Options;

namespace IdentityServerNET.ServerExtension.Default.Services.UI;

internal class DefaultUserInterfaceService : UserInterfaceService
{
    public DefaultUserInterfaceService(IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor)
        : base(optionsMonitor)
    {
    }
}
