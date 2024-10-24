using IdentityModel;
using IdentityServerNET.Token.ErrorHandling;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Extensions;

static public class JwtSecurityTokenExtensions
{
    static public JwtSecurityToken ToJwtSecurityToken(this string jwtEncodedString)
    {
        return new JwtSecurityToken(jwtEncodedString);
    }

    async static public Task<JwtSecurityToken> ToValidatedJwtSecurityToken(this string jwtEncodedString, string issuerUrl, string audience = null)
    {
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
               new ConfigurationManager<OpenIdConnectConfiguration>($"{issuerUrl}/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
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
        catch (Exception)
        {
            throw new TokenValidationException("Invalid token");
        }
        return jwtEncodedString.ToJwtSecurityToken();
    }

    async static public Task<string> GetValidatedClaimValue(this string jwtEncodedString, string issuerUrl, string claimType, string audience = null)
    {
        var jwtToken = await jwtEncodedString.ToValidatedJwtSecurityToken(issuerUrl);

        return jwtToken?
                    .Claims?
                    .Where(c => claimType.Equals(c.Type))
                    .FirstOrDefault()?
                    .Value;
    }

    async static public Task<IDictionary<string, string>> GetValidatedClaimsValue(this string jwtEncodedString, string issuerUrl, string[] claimTypes, string audience = null)
    {
        var result = new Dictionary<string, string>();

        if (claimTypes is null || claimTypes.Length == 0)
        {
            return result;
        }

        var jwtToken = await jwtEncodedString.ToValidatedJwtSecurityToken(issuerUrl);


        foreach (var claimType in claimTypes)
        {
            result[claimType] = jwtToken?
                    .Claims?
                    .Where(c => claimType.Equals(c.Type))
                    .FirstOrDefault()?
                    .Value;
        }

        return result;
    }

    static public JwtSecurityToken ToAssertionToken(this X509Certificate2 cert, string clientId, string issuer)
    {
        var now = DateTime.UtcNow;
        string tokenEndpoint = issuer + "/connect/token";

        var token = new JwtSecurityToken(
            clientId,
            tokenEndpoint,
            new List<Claim>
            {
                new Claim("jti", Guid.NewGuid().ToString()),
                new Claim(JwtClaimTypes.Subject, clientId),
                new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64)
            },
            now,
            now.AddMinutes(1),
            new SigningCredentials(
                new X509SecurityKey(cert),
                SecurityAlgorithms.RsaSha256
            )
        );

        return token;
    }

    static public string ToTokenString(this JwtSecurityToken token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);

        return tokenString;
    }

    static public byte[] BytesFromBase64(this string base64)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch { }

        try
        {
            return Convert.FromBase64String($"{base64}==");
        }
        catch { }

        return Convert.FromBase64String($"{base64}=");
    }
}
