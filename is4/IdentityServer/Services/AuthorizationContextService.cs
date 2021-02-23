using IdentityServer.Legacy.Services;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using IdentityServer.Legacy.Extensions;

namespace IdentityServer.Services
{
    public class AuthorizationContextService : IAuthorizationContextService
    {
        private readonly string _returnUrl;
        private readonly string _clientId;
        private readonly IIdentityServerInteractionService _interaction;

        private readonly string _authorization;

        public AuthorizationContextService(IHttpContextAccessor httpContextAccessor,
                                           IIdentityServerInteractionService interaction)
        {
            var httpRequest = httpContextAccessor?.HttpContext?.Request;
            _interaction = interaction;

            if (httpRequest != null)
            {

                _returnUrl = (httpRequest.HasFormContentType ? (string)httpRequest.Form["ReturnUrl"] : null) ?? httpRequest.Query["ReturnUrl"];
                _clientId = (httpRequest.HasFormContentType ? (string)httpRequest.Form["client_id"] : null) ?? httpRequest.Query["client_id"];

                if (String.IsNullOrEmpty(_returnUrl) &&
                    String.IsNullOrEmpty(_clientId) &&
                    !String.IsNullOrEmpty(httpRequest.Headers["Authorization"]) &&
                    httpRequest.Headers["Authorization"].ToString().ToLower().StartsWith("bearer "))  // get Client Id from Bearer Token
                {
                    try
                    {
                        var jwtToken = httpRequest.Headers["Authorization"].ToString().Substring("bearer ".Length);

                        if (jwtToken.Contains("."))
                        {
                            var payloadBase64 = jwtToken.Split('.')[1];

                            var tokenPayload = JsonConvert.DeserializeObject<TokenPayload>(Encoding.UTF8.GetString(payloadBase64.BytesFromBase64()));
                            _clientId = tokenPayload.client_id;
                        }
                    }
                    catch { }
                }
            }
            else
            {
                _returnUrl = _clientId = String.Empty;
            }
        }

        async public Task<AuthorizationContext> GetContextAsync()
        {
            if (!String.IsNullOrEmpty(_returnUrl))
            {
                var context = await _interaction.GetAuthorizationContextAsync(_returnUrl);

                return new AuthorizationContext()
                {
                    ReturnUrl = _returnUrl,
                    ClientId = context?.Client?.ClientId,
                    ClientName = context?.Client?.ClientName
                };
            }
            else if (!String.IsNullOrEmpty(_clientId))
            {
                return new AuthorizationContext()
                {
                    ClientId = _clientId
                };
            }
            return null;
        }

        public string GetClientId()
        {
            if (!String.IsNullOrEmpty(_returnUrl) && _returnUrl.Contains("?"))
            {
                var returnUrlQueryString = HttpUtility.ParseQueryString(_returnUrl.Substring(_returnUrl.IndexOf("?")));

                return returnUrlQueryString["client_id"] ?? String.Empty;
            }
            else if (!String.IsNullOrEmpty(_clientId))
            {
                return _clientId;
            }

            return String.Empty;
        }

        public string GetReturnUrlParameter(string parameter)
        {
            try
            {
                if (!String.IsNullOrEmpty(_returnUrl) && _returnUrl.Contains("?"))
                {
                    var queryString = HttpUtility.ParseQueryString(_returnUrl.Substring(_returnUrl.IndexOf("?")));

                    return queryString[parameter];
                }
            }
            catch { }

            return String.Empty;
        }

        #region Classes

        class TokenPayload
        {
            public string client_id { get; set; }
        }

        #endregion
    }
}
