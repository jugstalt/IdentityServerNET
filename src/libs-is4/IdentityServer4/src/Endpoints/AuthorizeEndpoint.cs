// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Configuration;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using Duende.IdentityModel;

namespace IdentityServer4.Endpoints;

internal class AuthorizeEndpoint : AuthorizeEndpointBase
{
    private readonly IPushedAuthorizationRequestStore _parStore;
    private readonly IClientStore _clientStore;

    public AuthorizeEndpoint(
       IEventService events,
       ILogger<AuthorizeEndpoint> logger,
       IdentityServerOptions options,
       IAuthorizeRequestValidator validator,
       IAuthorizeInteractionResponseGenerator interactionGenerator,
       IAuthorizeResponseGenerator authorizeResponseGenerator,
       IUserSession userSession,
       IPushedAuthorizationRequestStore parStore = null,
       IClientStore clientStore = null)
        : base(events, logger, options, validator, interactionGenerator, authorizeResponseGenerator, userSession)
    {
        _parStore = parStore;
        _clientStore = clientStore;
    }

    public override async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        Logger.LogDebug("Start authorize request");

        NameValueCollection values;

        if (HttpMethods.IsGet(context.Request.Method))
        {
            values = context.Request.Query.AsNameValueCollection();
        }
        else if (HttpMethods.IsPost(context.Request.Method))
        {
            if (!context.Request.HasApplicationFormContentType())
            {
                return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
            }

            values = context.Request.Form.AsNameValueCollection();
        }
        else
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        // PAR: check RequirePushedAuthorization on client
        var requestUri = values["request_uri"];
        if (requestUri == null)
        {
            var clientId = values["client_id"];
            if (clientId != null)
            {
                var client = await _clientStore.FindClientByIdAsync(clientId);
                if (client != null && client.RequirePushedAuthorization)
                {
                    Logger.LogWarning("Client {clientId} requires PAR but request_uri is missing", clientId);
                    return new TokenErrorResult(new TokenErrorResponse
                    {
                        Error = OidcConstants.AuthorizeErrors.InvalidRequest,
                        ErrorDescription = "client requires pushed authorization requests"
                    });
                }
            }
        }

        // PAR: resolve request_uri to stored parameters — only for PAR URNs, not JAR http(s):// URIs
        if (requestUri.IsParRequestUri())
        {
            var storedParams = await _parStore.GetAsync(requestUri);
            if (storedParams == null)
            {
                Logger.LogWarning("PAR request_uri not found or expired: {requestUri}", requestUri);
                return new TokenErrorResult(new TokenErrorResponse
                {
                    Error = "invalid_request",
                    ErrorDescription = "request_uri expired or not found"
                });
            }

            // consume – request_uri is single-use per RFC 9126
            await _parStore.RemoveAsync(requestUri);

            // keep client_id from query string (required by spec), merge with stored params
            var merged = new NameValueCollection(storedParams);
            var clientId = values["client_id"];
            if (clientId != null)
                merged["client_id"] = clientId;

            values = merged;
            Logger.LogDebug("PAR: resolved request_uri to stored parameters for client {clientId}", merged["client_id"]);
        }

        var user = await UserSession.GetUserAsync();
        var result = await ProcessAuthorizeRequestAsync(values, user, null);

        Logger.LogTrace("End authorize request. result type: {0}", result?.GetType().ToString() ?? "-none-");

        return result;
    }
}
