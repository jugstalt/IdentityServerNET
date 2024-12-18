﻿using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.Signing;

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

    public IdentityServer4.Models.Token CreateCustomToken(NameValueCollection claims, int lifeTime = 3600)
    {
        var token = new IdentityServer4.Models.Token()
        {
            AccessTokenType = AccessTokenType.Jwt,
            Lifetime = lifeTime,
            Issuer = AppUrl(),
            Claims = claims.AllKeys?
                           .Select(k => new Claim(k, claims[k]))
                           .ToArray()
        };

        return token;
    }

    public IdentityServer4.Models.Token CreateUserToken(ApplicationUser user, int lifeTime = 3600)
    {
        var claims = new NameValueCollection()
        {
            { JwtClaimTypes.Id, user.Id },
            { JwtClaimTypes.Name, user.UserName}
        };

        foreach (var role in user.Roles ?? [])
        {
            claims.Add(JwtClaimTypes.Role, role);
        }

        return CreateCustomToken(claims, lifeTime);
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
        if (!String.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor;
        }

        return $"{_httpContext.Request.Scheme}://{_httpContext.Request.Host}"; // {Context.Request.Path}{Context.Request.QueryString}")
    }

    #endregion
}
