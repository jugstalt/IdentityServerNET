using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace IdentityServer.Nova.Services.UI;

public class UserInterfaceService : IUserInterfaceService
{
    private readonly UserInterfaceServiceOptions _options;

    public UserInterfaceService(IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor)
    {
        _options = optionsMonitor.CurrentValue;
    }

    virtual public string ApplicationTitle => _options.ApplicationTitle;

    virtual public string OverrideCssContent => _options.OverrideCssContent;

    virtual public IDictionary<string, byte[]> MediaContent => _options.MediaContent;

    virtual public string LoginLayoutBodyClass => String.Empty;

    virtual public bool DenyForgotPasswordChallange => false;
}
