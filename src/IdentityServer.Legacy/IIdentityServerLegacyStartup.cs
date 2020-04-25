using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy
{
    public interface IIdentityServerLegacyStartup
    {
        void ConfigureServices(WebHostBuilderContext context, IServiceCollection services);

        string CssOverrides();
    }
}
