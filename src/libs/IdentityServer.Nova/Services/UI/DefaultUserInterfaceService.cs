using Microsoft.Extensions.Options;

namespace IdentityServer.Nova.Services.UI;
public class DefaultUserInterfaceService : UserInterfaceService
{
    public DefaultUserInterfaceService(IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor)
        : base(optionsMonitor)
    {
    }
}
