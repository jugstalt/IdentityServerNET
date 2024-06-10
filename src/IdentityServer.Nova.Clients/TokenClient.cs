using IdentityModel.Client;
using IdentityServer.Nova.Extensions;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Clients
{
    public class TokenClient
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly X509Certificate2 _clientCertificate = null;

        public TokenClient(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public TokenClient(string clientId, X509Certificate2 clientCertificate)
        {
            _clientId = clientId;
            _clientCertificate = clientCertificate;
        }

        public string AccessToken { get; private set; }

        // reuse http client
        private HttpClient _httpClient = null;
        protected HttpClient GetHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }

            return _httpClient;
        }

        async public Task GetAccessToken(string identityServerAddress, string[] scopes)
        {
            var httpClient = GetHttpClient();
            var disco = await httpClient.GetDiscoveryDocumentAsync(identityServerAddress);

            if (disco.IsError)
            {
                throw new Exception(disco.Error);
            }

            TokenResponse tokenResponse;
            if (_clientCertificate != null)
            {
                var clientAssertion = new ClientAssertion()
                {
                    Type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    Value = _clientCertificate.ToAssertionToken(_clientId, identityServerAddress).ToTokenString()
                };

                tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,

                    ClientId = _clientId,
                    ClientAssertion = clientAssertion,

                    Scope = String.Join(" ", scopes)
                });
            }
            else
            {
                tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,

                    ClientId = _clientId,
                    ClientSecret = _clientSecret,

                    Scope = String.Join(" ", scopes)
                });
            }

            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Error);
            }

            AccessToken = tokenResponse.AccessToken;
        }
    }
}
