# Implementing OpenIdConnect using ConfigurationExtensions

This documentation will guide you through setting up OpenIdConnect authentication in your ASP.NET Core application using the `ConfigurationExtensions` class for better configuration management.

## Configuration

First, ensure your configuration file (e.g., `appsettings.json`) includes the necessary settings for OpenIdConnect:

```json
{
    "IdentityFromOpenIdConnect": {
        "Authority": "https://your-authority-url.com",
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret",
        "RequireHttpsMetadata": "false", // Optional, default is false
        "GetClaimsFromUserInfoEndpoint": "true", // Optional, default is true
        "SaveTokens": "true", // Optional, default is true
        "Scopes": "openid,profile,email", // Optional, default is empty
        "NameClaim": "name", // Optional, default is null
        "RoleClaim": "role", // Optional, default is null
        "MapAllClaimActionsExcept": "iss,nbf,exp,aud,nonce,iat,c_hash" // Optional, default is a predefined list
    }
}
```

## Implementing OpenIdConnect

Update your `Startup.cs` (or wherever you configure services) to use the `ConfigurationExtensions` for setting up OpenIdConnect:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    IConfiguration configuration = ... // Get your configuration instance

    if (configuration.UseIdentityFromOpenIdConnect())
    {
        Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie("Cookies", options => {})
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = configuration.OpenIdConnectAuthority();
            options.RequireHttpsMetadata = configuration.OpenIdConnectRequireHttpsMetadata();

            options.ClientId = configuration.OpenIdConnectClientId();
            options.ClientSecret = configuration.OpenIdConnectClientSecret();
            options.ResponseType = "code";

            options.GetClaimsFromUserInfoEndpoint = configuration.OpenIdConnectGetClaimsFromUserInfoEndpoint();
            options.SaveTokens = configuration.OpenIdConnectSaveTokens();

            options.Scope.Clear();
            foreach (var scope in configuration.OpenIdConnectScopes())
            {
                options.Scope.Add(scope);
            }

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
            {
                NameClaimType = configuration.OpenIdConnectNameClaim() ?? "name",
                RoleClaimType = configuration.OpenIdConnectRoleClaimType() ?? "role"
            };

            options.ClaimActions.MapAllExcept(configuration.OpenIdConnectMapAllClaimActionsExcept());
        });
    }
}
```

## Explanation

1. **Configuration File**: Ensure `appsettings.json` or another configuration file includes the necessary OpenIdConnect settings.
2. **Use ConfigurationExtensions**: Replace direct configuration settings with the provided extension methods for better readability and maintainability.
3. **Authentication Setup**: The `ConfigureServices` method in `Startup.cs` should utilize these extensions to configure OpenIdConnect authentication.

### Configuration Methods

-   **UseIdentityFromOpenIdConnect**: Checks if the necessary OpenIdConnect settings are present.
-   **OpenIdConnectAuthority**: Retrieves the authority URL.
-   **OpenIdConnectClientId**: Retrieves the client ID.
-   **OpenIdConnectClientSecret**: Retrieves the client secret.
-   **OpenIdConnectRequireHttpsMetadata**: Checks if HTTPS metadata is required (default: false).
-   **OpenIdConnectGetClaimsFromUserInfoEndpoint**: Checks if claims should be retrieved from the user info endpoint (default: true).
-   **OpenIdConnectSaveTokens**: Checks if tokens should be saved (default: true).
-   **OpenIdConnectScopes**: Retrieves the scopes as an array (default: empty array).
-   **OpenIdConnectNameClaim**: Retrieves the name claim type (default: null).
-   **OpenIdConnectRoleClaimType**: Retrieves the role claim type (default: null).
-   **OpenIdConnectMapAllClaimActionsExcept**: Retrieves the array of claim actions to exclude from mapping (default: `["iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash"]`).

## Conclusion

By following this guide, you will set up OpenIdConnect authentication in your ASP.NET Core application using a structured and maintainable approach provided by `ConfigurationExtensions`. This will enhance the manageability and readability of your configuration settings.
