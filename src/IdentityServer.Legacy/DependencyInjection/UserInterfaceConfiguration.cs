using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.DependencyInjection
{
    public class UserInterfaceConfiguration
    {
        public string ApplicationTitle { get; set; }

        public string OverrideCssContent { get; set; }

        public IDictionary<string, byte[]> MediaContent { get; set; }
    }
}
