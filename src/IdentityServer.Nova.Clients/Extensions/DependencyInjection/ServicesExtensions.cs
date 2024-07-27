using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Clients.Extensions.DependencyInjection;

static public class ServicesExtensions
{
    private const string Authority = "IdentityFromOpenIdConnect:Authority";
    private const string ClientId = "IdentityFromOpenIdConnect:ClientId";
    private const string ClientSecret = "IdentityFromOpenIdConnect:ClientId";

    static public IServiceCollection AddIdentityFromOpenIdConnect(this IServiceCollection services,
                                     IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration[Authority]) ||
            string.IsNullOrEmpty(configuration[ClientId]) ||
            string.IsNullOrEmpty(configuration[ClientSecret]))
        {
            return services;
        }

        Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

        //services
            //.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = "Cookies";
            //    options.DefaultChallengeScheme = "oidc";
            //})
            //.AddCookie("Cookies", options =>
            //{
            //})
            //.AddOpenIdConnect("oidc", options =>
            //{
            //    options.Authority = securityConfig.OpenIdConnectConfiguration.Authority;
            //    options.RequireHttpsMetadata = false;

            //    options.ClientId = securityConfig.OpenIdConnectConfiguration.ClientId;
            //    options.ClientSecret = securityConfig.OpenIdConnectConfiguration.ClientSecret;
            //    options.ResponseType = "code";

            //    options.GetClaimsFromUserInfoEndpoint = true;

            //    options.SaveTokens = true;

            //    options.Scope.Clear();
            //    foreach (var scope in securityConfig.OpenIdConnectConfiguration.Scopes ?? new string[] { "openid", "profile" })
            //    {
            //        options.Scope.Add(scope);
            //    }

            //    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
            //    {
            //        NameClaimType = "name",
            //        RoleClaimType = "role"
            //    };

            //    options.ClaimActions.MapAllExcept("iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash");
            //});

        return services;
    }
}
