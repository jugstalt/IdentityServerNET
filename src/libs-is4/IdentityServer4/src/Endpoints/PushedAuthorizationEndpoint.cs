using Duende.IdentityModel;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Endpoints;

/// <summary>
/// Pushed Authorization Request endpoint (RFC 9126) – POST /connect/par
/// </summary>
internal class PushedAuthorizationEndpoint : IEndpointHandler
{
    private const int ExpiresIn = 60;

    private readonly IClientSecretValidator _clientValidator;
    private readonly IAuthorizeRequestValidator _authorizeRequestValidator;
    private readonly IPushedAuthorizationRequestStore _parStore;
    private readonly ILogger<PushedAuthorizationEndpoint> _logger;

    public PushedAuthorizationEndpoint(
        IClientSecretValidator clientValidator,
        IAuthorizeRequestValidator authorizeRequestValidator,
        IPushedAuthorizationRequestStore parStore,
        ILogger<PushedAuthorizationEndpoint> logger)
    {
        _clientValidator = clientValidator;
        _authorizeRequestValidator = authorizeRequestValidator;
        _parStore = parStore;
        _logger = logger;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.LogTrace("Processing pushed authorization request.");

        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasApplicationFormContentType())
        {
            _logger.LogWarning("Invalid HTTP request for PAR endpoint");
            return Error(OidcConstants.TokenErrors.InvalidRequest);
        }

        return await ProcessPushedAuthorizationRequestAsync(context);
    }

    private async Task<IEndpointResult> ProcessPushedAuthorizationRequestAsync(HttpContext context)
    {
        _logger.LogDebug("Start pushed authorization request.");

        // validate client credentials
        var clientResult = await _clientValidator.ValidateAsync(context);
        if (clientResult.Client == null)
        {
            _logger.LogWarning("PAR: client validation failed");
            return Error(OidcConstants.TokenErrors.InvalidClient);
        }

        // read form parameters
        var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();

        // reject any incoming request_uri — PAR cannot be nested
        if (form["request_uri"] != null)
        {
            return Error(OidcConstants.AuthorizeErrors.InvalidRequest, "request_uri not allowed in PAR request");
        }

        // validate as if it were a normal authorize request (validates scope, redirect_uri, response_type, PKCE etc.)
        var user = context.User;
        var validationResult = await _authorizeRequestValidator.ValidateAsync(form, user?.Identity?.IsAuthenticated == true ? user : null);

        if (validationResult.IsError)
        {
            _logger.LogWarning("PAR: authorize request validation failed: {error}", validationResult.Error);
            return Error(validationResult.Error, validationResult.ErrorDescription);
        }

        // generate unique request_uri
        var requestUri = "urn:ietf:params:oauth:request_uri:" + CryptoRandom.CreateUniqueId(32);

        // strip client authentication parameters — these must not leak into the authorize flow
        var authorizationParams = form.AllKeys
            .Where(k => k != null && k != "client_secret" && k != "client_assertion" && k != "client_assertion_type")
            .Aggregate(new System.Collections.Specialized.NameValueCollection(), (nvc, k) => { nvc[k] = form[k]; return nvc; });

        await _parStore.StoreAsync(requestUri, authorizationParams, ExpiresIn);

        _logger.LogDebug("PAR request stored, request_uri={requestUri}", requestUri);

        return new PushedAuthorizationResult(requestUri, ExpiresIn);
    }

    private TokenErrorResult Error(string error, string errorDescription = null)
    {
        var response = new TokenErrorResponse
        {
            Error = error,
            ErrorDescription = errorDescription
        };

        _logger.LogError("PAR error: {error} - {description}", error, errorDescription ?? "-");

        return new TokenErrorResult(response);
    }
}
