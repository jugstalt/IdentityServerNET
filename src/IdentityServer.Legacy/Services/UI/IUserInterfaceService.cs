using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.UI
{
    public interface IUserInterfaceService
    {
        string ApplicationTitle { get; }

        string OverrideCssContent { get; }

        IDictionary<string, byte[]> MediaContent { get; }

        string LoginLayoutBodyClass { get; }

        bool DenyForgotPasswordChallange { get; }
    }
}
