using Microsoft.Extensions.Options;

namespace IdentityServerNET.Services.UI;
public class DefaultUserInterfaceService : UserInterfaceService
{
    public DefaultUserInterfaceService(IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor)
        : base(optionsMonitor)
    {
    }
}
