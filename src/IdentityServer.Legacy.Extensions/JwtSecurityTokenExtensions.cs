using IdentityServer.Legacy.Token.ErrorHandling;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Extensions
{
    static public class JwtSecurityTokenExtensions
    {
        static public JwtSecurityToken ToJwtSecurityToken(this string jwtEncodedString)
        {
            return new JwtSecurityToken(jwtEncodedString);
        }

        async static public Task<JwtSecurityToken> ToValidatedJwtSecurityToken(this string jwtEncodedString, string issuerUrl, string audience = null)
        {
            IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
                   new ConfigurationManager<OpenIdConnectConfiguration>($"{ issuerUrl }/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            OpenIdConnectConfiguration openIdConfiguration = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters()
            {
                ValidIssuer = issuerUrl,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                ValidateAudience = !String.IsNullOrEmpty(audience),
                IssuerSigningKeys = openIdConfiguration.SigningKeys
            };

            try
            {
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(jwtEncodedString, validationParameters, out validatedToken);
            } 
            catch(Exception)
            {
                throw new TokenValidationException("Invalid token");
            }
            return jwtEncodedString.ToJwtSecurityToken();
        }
    }
}
