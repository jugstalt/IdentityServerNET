using IdentityServer.Legacy.Services.UI;
using IdentityServer.Legacy.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

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
