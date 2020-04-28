using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityServer.Legacy.Extensions;
using System.Security.Cryptography.X509Certificates;

namespace ClientApp
{
    class Program
    {
        async static Task Main(string[] args)
        {

            await CallSecretsVault("webgis", "secret", "webgis/sec1");
            return;

            var cert = new X509Certificate2(@"C:\temp\identityserver_legacy\cert.pfx", "");

            string issuer = "https://localhost:44300";
            var tokenResponse = /*await GetToken()*/ /*await GetUserToken()*/ await GetToken(cert);

            //if(tokenResponse == null)
            //{
            //    return;
            //}

            try
            {
                //var token = new OpenIdServer.Client.Token("https://localhost:44324", "eyJhbGciOiJSUzI1NiIsImtpZCI6ImV6Y18yQ3NRRkNBR2h0R3ZwNmg1ZHciLCJ0eXAiOiJhdCtqd3QifQ.eyJuYmYiOjE1ODU1Njc5NzQsImV4cCI6MTU4NTU3MTU3NCwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzMjQiLCJhdWQiOlsiYXBpMSIsImFwaTIiXSwiY2xpZW50X2lkIjoiY2xpZW50Iiwic2NvcGUiOlsiYXBpMSIsImFwaTIiXX0.U2gg7N-xNuxXVMIH7rFvdAUooF4JWXJw8Bpv5RMLt1yD5lM_6KAXUNO62p6UCCMXOVdixJCxSkd0e51IAzEPksoH989mTDL7JsGQJ7EqsKiW1VIEmpU2Yiprt-8ejBml85fuV9HIGDoDe9tbzKHPNuIaXEPspSeZp0NxhR6YBvLyQ2j9Az1D-OKTHn2VxDALiyWix1k4XoCo7txULHwZfTPmzwXXKq_czsACNfAyhsqikbQtwNL6I7RbFAq8wSepZU3AnSdupsYdjvdjANVi_diIgu7f1NdSMu7zn1AUGzvV2ogdgBQZgHH3fcL2lJUYcrs01Cqfhuhnvq-b79oo2w");

                //var token = new OpenIdServer.Client.Token("https://localhost:44324");
                //await token.RequestClientCredentialsTokenAsync("client", "secret1", "api1 api2");

                Console.WriteLine(tokenResponse.Json);

                if (await tokenResponse.AccessToken.ToValidatedJwtSecurityToken(issuer) == null)
                {
                    Console.WriteLine("Token not valid");
                    return;
                }

                Console.WriteLine("Token successfully verified!");

                await GetUserInfo(tokenResponse);
                //await VerifyToken(tokenResponse);
                await CallApi(tokenResponse.AccessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excepiton: " + ex.Message);
            }

            Console.ReadLine();
        }

        async static Task<TokenResponse> GetToken()
        {
            // discover endpoints from metadata
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:44300");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return null;
            }

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = "client",
                ClientSecret = "secret1",
                
                Scope = "api1"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return null;
            }

            return tokenResponse;
        }

        async static Task<TokenResponse> GetToken(X509Certificate2 cert)
        {
            // discover endpoints from metadata
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:44300");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return null;
            }

            var clientAssertion = new ClientAssertion()
            {
                Type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                Value = cert.ToAssertionToken("client", "https://localhost:44300").ToTokenString()
            };

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = "client",
                ClientAssertion=  clientAssertion,

                Scope = "api1",
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return null;
            }

            return tokenResponse;
        }

        async static Task<TokenResponse> GetUserToken()
        {
            // discover endpoints from metadata
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:44300");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return null;
            }

            // request token
            var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest()
            {
                Address = disco.TokenEndpoint,

                ClientId = "client",
                ClientSecret = "secret1",

                UserName="test@xyz.com",
                Password="Pwd#123",

                Scope = "openid profile api1 api2"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return null;
            }

            return tokenResponse;
        }

        async static Task GetUserInfo(TokenResponse tokenResponse)
        {
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:44300");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }

            var response = await client.GetUserInfoAsync(new UserInfoRequest
            {
                Address = disco.UserInfoEndpoint,
                Token = tokenResponse.AccessToken
            });

            Console.WriteLine("User Info:");
            if (response.IsError)
            {
                Console.WriteLine("Error: " + response.Error);
                return;
            }

            foreach(var claim in response.Claims)
            {
                Console.WriteLine($"{ claim.Type } = { claim.Value }");
            }
        }

        async static Task CallApi(string accessToken)
        {
            // call api
            var client = new HttpClient();
            client.SetBearerToken(accessToken);

            var response = await client.GetAsync("https://localhost:44353/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }
        }

        async static Task CallSecretsVault(string clientName, string secret, string path)
        {
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:44300");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = clientName,
                ClientSecret = secret,

                Scope = "secrets-vault secrets-vault.webgis"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine("Access-Token: " + tokenResponse.AccessToken);

            client.SetBearerToken(tokenResponse.AccessToken);

            var response = await client.GetAsync("https://localhost:44300/api/secretsvault?path=" + path);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Secret:");

                Console.WriteLine(content);
            }

            Console.ReadLine();
        }
    }
}
