using IdentityModel.Client;
using IdentityServer.Nova.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Clients;

public class SigningApiClient : TokenClient
{
    public SigningApiClient(string clientId, string clientSecret, HttpClient httpClient = null)
        : base(clientId, clientSecret, httpClient)
    {

    }

    public SigningApiClient(string clientId, X509Certificate2 clientCertificate, HttpClient httpClient = null)
        : base(clientId, clientCertificate, httpClient)
    {

    }

    public Task<SigningApiResponse> SignData(string identityServerAddress, string tokenData, int lifeTime = 3600)
    {
        var nvc = new NameValueCollection();
        nvc["token-data"] = tokenData;

        return SignData(identityServerAddress, nvc, lifeTime);
    }

    async public Task<SigningApiResponse> SignData(string identityServerAddress, NameValueCollection claims, int lifeTime = 3600)
    {
        try
        {
            await GetAccessToken(identityServerAddress, new string[] { "signing-api" });

            var httpClient = GetHttpClient();
            httpClient.SetBearerToken(this.AccessToken);


            using (var httpContext = new FormUrlEncodedContent(claims.AllKeys?
                                                                     .Select(k => new KeyValuePair<string, string>(k, claims[k]))
                                                                     .ToArray()))
            {
                var httpResponse = await httpClient.PostAsync($"{identityServerAddress}/api/signing?lifetime={lifeTime}", httpContext);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<SigningApiResponse>(jsonResponse);
                }
                else
                {
                    throw new Exception($"Connection to singing-api failed. Status code: {httpResponse.StatusCode}");
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
