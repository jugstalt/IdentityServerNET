using IdentityServerNET.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IdentityServerNET.Services;

public class AuthorizationContextService : IAuthorizationContextService
{
    private readonly string _returnUrl;
    private readonly string _clientId;
    private readonly IIdentityServerInteractionService _interaction;

    public AuthorizationContextService(IHttpContextAccessor httpContextAccessor,
                                       IIdentityServerInteractionService interaction)
    {
        var httpRequest = httpContextAccessor?.HttpContext?.Request;
        _interaction = interaction;

        if (httpRequest != null)
        {

            _returnUrl = (httpRequest.HasFormContentType ? (string)httpRequest.Form["ReturnUrl"] : null) ?? httpRequest.Query["ReturnUrl"];
            _clientId = (httpRequest.HasFormContentType ? (string)httpRequest.Form["client_id"] : null) ?? httpRequest.Query["client_id"];

            if (string.IsNullOrEmpty(_returnUrl) &&
                string.IsNullOrEmpty(_clientId) &&
                !string.IsNullOrEmpty(httpRequest.Headers["Authorization"]) &&
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
            _returnUrl = _clientId = string.Empty;
        }
    }

    async public Task<AuthorizationContext> GetContextAsync()
    {
        if (!string.IsNullOrEmpty(_returnUrl))
        {
            var context = await _interaction.GetAuthorizationContextAsync(_returnUrl);

            return new AuthorizationContext()
            {
                ReturnUrl = _returnUrl,
                ClientId = context?.Client?.ClientId,
                ClientName = context?.Client?.ClientName
            };
        }
        else if (!string.IsNullOrEmpty(_clientId))
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
        if (!string.IsNullOrEmpty(_returnUrl) && _returnUrl.Contains("?"))
        {
            var returnUrlQueryString = HttpUtility.ParseQueryString(_returnUrl.Substring(_returnUrl.IndexOf("?")));

            return returnUrlQueryString["client_id"] ?? string.Empty;
        }
        else if (!string.IsNullOrEmpty(_clientId))
        {
            return _clientId;
        }

        return string.Empty;
    }

    public string GetReturnUrlParameter(string parameter)
    {
        try
        {
            if (!string.IsNullOrEmpty(_returnUrl) && _returnUrl.Contains("?"))
            {
                var queryString = HttpUtility.ParseQueryString(_returnUrl.Substring(_returnUrl.IndexOf("?")));

                return queryString[parameter];
            }
        }
        catch { }

        return string.Empty;
    }

    #region Classes

    class TokenPayload
    {
        public string client_id { get; set; }
    }

    #endregion
}
