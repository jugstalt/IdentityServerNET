using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    public interface IBuilder
    {
        IServiceCollection Services { get; }
    }
}
