using IdentityModel.Client;
using IdentityServer.Legacy.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Clients
{
    public class SigningApiClient : TokenClient
    {
        public SigningApiClient(string clientId, string clientSecret)
            : base(clientId, clientSecret)
        {

        }

        public SigningApiClient(string clientId, X509Certificate2 clientCertificate)
            : base(clientId, clientCertificate)
        {

        }

        async public Task<SigningApiResponse> SignData(string identityServerAddress, string data)
        {
            try
            {
                await GetAccessToken(identityServerAddress, new string[] { "signing-api" });

                var httpClient = GetHttpClient();
                httpClient.SetBearerToken(this.AccessToken);

                using (var httpContext = new FormUrlEncodedContent(new[]
                                            {
                                                new KeyValuePair<string, string>("data", data)
                                            }))
                {
                    var httpResponse = await httpClient.PostAsync($"{ identityServerAddress }/api/signing", httpContext);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                        return JsonConvert.DeserializeObject<SigningApiResponse>(jsonResponse);
                    }
                    else
                    {
                        throw new Exception($"Connection to singing-api failed. Status code: { httpResponse.StatusCode }");
                    }
                }
            }
            catch (Exception ex)
            {
                return new SigningApiResponse()
                {
                    Succeded = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #region Static Members

        async static public Task<string> GetValidatedDataFromToken(string token, string identityServerAddress)
        {
            return await token.GetValidatedClaimValue(identityServerAddress, "token-data");
        }

        #endregion
    }
}
