using System.Collections.Generic;

namespace IdentityServerNET.Abstractions.UI;

public interface IUserInterfaceService
{
    string ApplicationTitle { get; }

    string OverrideCssContent { get; }

    IDictionary<string, byte[]> MediaContent { get; }

    string LoginLayoutBodyClass { get; }

    bool DenyForgotPasswordChallange { get; }
}
