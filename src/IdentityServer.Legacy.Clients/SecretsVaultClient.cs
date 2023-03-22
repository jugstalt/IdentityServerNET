using IdentityModel.Client;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Clients
{
    public class SecretsVaultClient : TokenClient
    {
        public SecretsVaultClient(string clientId, string clientSecret)
            : base(clientId, clientSecret)
        {

        }

        public SecretsVaultClient(string clientId, X509Certificate2 clientCertificate)
            : base(clientId, clientCertificate)
        {

        }

        private string _currentLocker;
        private string _currentIdentityServerAddress;
        async public Task OpenLocker(string identityServerAddress, string lockerName)
        {
            if (!lockerName.Equals(_currentLocker))
            {
                await GetAccessToken(_currentIdentityServerAddress = identityServerAddress, new string[] { "secrets-vault", $"secrets-vault.{_currentLocker = lockerName}" });
            }
        }

        async public Task<SecretsVaultResponse> GetSecret(string secretName, long versionTimeStamp = 0)
        {
            if (String.IsNullOrEmpty(_currentLocker) || String.IsNullOrEmpty(this.AccessToken))
            {
                throw new Exception("No current locker or no access to locker. Please successfully run \"OpenLocker\" methode first");
            }

            string path = versionTimeStamp > 0 ? $"{_currentLocker}/{secretName}/{versionTimeStamp}" : $"{_currentLocker}/{secretName}";

            var httpClient = GetHttpClient();
            httpClient.SetBearerToken(this.AccessToken);

            var response = await httpClient.GetAsync($"{_currentIdentityServerAddress}/api/secretsvault?v=1.0&path={path}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Secret request failed with statuscode {response.StatusCode}");
            }
            else
            {
                var secretJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SecretsVaultResponse>(secretJson);
            }
        }

        public object OpenLocker(string secretsVaultServer, object p)
        {
            throw new NotImplementedException();
        }
    }
};
