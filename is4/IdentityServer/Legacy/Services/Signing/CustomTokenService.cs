using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.Signing
{
    public class CustomTokenService : DefaultTokenService
    {
        private HttpContext _httpContext;

        public CustomTokenService(
            IClaimsService claimsProvider,
            IReferenceTokenStore referenceTokenStore,
            ITokenCreationService creationService,
            IHttpContextAccessor contextAccessor,
            ISystemClock clock,
            IKeyMaterialService keyMaterialService,
            IOptionsMonitor<IdentityServerOptions> options,
            ILogger<DefaultTokenService> logger) : base(claimsProvider, referenceTokenStore, creationService, contextAccessor, clock, keyMaterialService, options.CurrentValue, logger)
        {
            _httpContext = contextAccessor.HttpContext;
        }

        //public override async Task<IdentityServer4.Models.Token> CreateIdentityTokenAsync(TokenCreationRequest request)
        //{
        //    _idTokenRequest = request;

        //    var id_token = await base.CreateIdentityTokenAsync(request);

        //    // store token and associated user info
        //    string client_id = request.ValidatedRequest.Client.ClientId;
        //    _sessionId = request.ValidatedRequest.SessionId;
        //    string user_id = request.Subject.GetSubjectId();

        //    return id_token;
        //}


        public IdentityServer4.Models.Token CreateCustomToken(string data)
        {
            var token = new IdentityServer4.Models.Token()
            {
                AccessTokenType = AccessTokenType.Jwt,
                Lifetime = 3600,
                Issuer = AppUrl(),
                Claims = new List<Claim>(new Claim[]
                {
                    new Claim("token-data", data)
                })
            };

            return token;
        }

        public override async Task<string> CreateSecurityTokenAsync(IdentityServer4.Models.Token token)
        {
            string tokenString = await base.CreateSecurityTokenAsync(token);
            return tokenString;
        }

        #region Helper

        private string AppUrl()
        {
            var forwardedFor = _httpContext?.Request.Headers["X-Forwarded-For"].ToString();
            if(!String.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor;
            }

            return $"{_httpContext.Request.Scheme}://{_httpContext.Request.Host}"; // {Context.Request.Path}{Context.Request.QueryString}")
        }

        #endregion
    }
}
