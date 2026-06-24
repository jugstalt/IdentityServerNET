using IdentityServerNET.Abstractions.UI;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace IdentityServerNET.Services.UI;

public class UserInterfaceService : IUserInterfaceService
{
    private readonly UserInterfaceServiceOptions _options;
    private readonly UICustomizationService? _customization;

    public UserInterfaceService(
        IOptionsMonitor<UserInterfaceServiceOptions> optionsMonitor,
        UICustomizationService? customization = null)
    {
        _options = optionsMonitor.CurrentValue;
        _customization = customization;
    }

    virtual public string ApplicationTitle
    {
        get
        {
            var custom = _customization?.LoadAsync().GetAwaiter().GetResult().ApplicationTitle;
            return string.IsNullOrEmpty(custom) ? _options.ApplicationTitle : custom;
        }
    }

    virtual public string OverrideCssContent => _options.OverrideCssContent;

    virtual public IDictionary<string, byte[]> MediaContent => _options.MediaContent;

    virtual public string LoginLayoutBodyClass => String.Empty;

    virtual public bool DenyForgotPasswordChallange => false;
}
