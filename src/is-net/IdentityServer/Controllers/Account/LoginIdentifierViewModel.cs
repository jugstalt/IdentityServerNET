using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer;

public class LoginIdentifierViewModel : LoginIdentifierInputModel
{
    public bool EnableLocalLogin { get; set; } = true;
    public bool AllowPasskeyLogin { get; set; }
    public IEnumerable<ExternalProvider> ExternalProviders { get; set; } = Enumerable.Empty<ExternalProvider>();
    public IEnumerable<ExternalProvider> VisibleExternalProviders => ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));
    public bool IsExternalLoginOnly => !EnableLocalLogin && ExternalProviders?.Count() == 1;
    public string ExternalLoginScheme => IsExternalLoginOnly ? ExternalProviders?.SingleOrDefault()?.AuthenticationScheme : null;
}
