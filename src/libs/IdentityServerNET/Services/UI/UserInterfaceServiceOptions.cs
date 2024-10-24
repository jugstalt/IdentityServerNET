using System.Collections.Generic;

namespace IdentityServerNET.Services.UI;

public class UserInterfaceServiceOptions
{
    public string ApplicationTitle { get; set; }

    public string OverrideCssContent { get; set; }

    public IDictionary<string, byte[]> MediaContent { get; set; }
}
