using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection;

static public class ServicesExtensions
{
    private const string ConfigurationSectionName = "OpenIdConnectAuthentication";
    private const string Authority = "Authority";
    private const string ClientId = "ClientId";

    static public IServiceCollection OpenIdConnectAuthentication(
                                     this IServiceCollection services,
                                     IConfiguration configuration
                                )
     => services.OpenIdConnectAuthentication(configuration.GetSection(ConfigurationSectionName));
    

    static public IServiceCollection OpenIdConnectAuthentication(
                                     this IServiceCollection services,
                                     IConfigurationSection configuration
                                )
    {
        if (string.IsNullOrEmpty(configuration[Authority]) ||
            string.IsNullOrEmpty(configuration[ClientId]))
        {
            return services;
        }

        Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies", options =>
            {
            })
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

                if (!String.IsNullOrEmpty(configuration.OpenIdConnectNameClaim()) ||
                    !String.IsNullOrEmpty(configuration.OpenIdConnectRoleClaimType())
                    )
                {
                    var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters();
                    if (!String.IsNullOrEmpty(configuration.OpenIdConnectNameClaim()))
                    {
                        validationParameters.NameClaimType = configuration.OpenIdConnectNameClaim();
                    }

                    if (!String.IsNullOrEmpty(configuration.OpenIdConnectRoleClaimType()))
                    {
                        validationParameters.RoleClaimType = configuration.OpenIdConnectRoleClaimType();
                    }

                    options.TokenValidationParameters = validationParameters;
                }

                options.ClaimActions.MapAllExcept(configuration.OpenIdConnectMapAllClaimActionsExcept());
            });

        return services;
    }
}
