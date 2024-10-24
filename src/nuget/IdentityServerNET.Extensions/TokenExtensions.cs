//using IdentityModel;
//using IdentityModel.Client;
//using IdentityServerNET.Token.ErrorHandling;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Concurrent;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace IdentityServerNET.Token
//{
//    public static class TokenExtensions
//    {
//        async static public Task<bool> VerifyAccessToken(this string accessToken, string issuer)
//        {
//            if (String.IsNullOrEmpty(accessToken))
//            {
//                throw new Exception("Token not intialized");
//            }

//            if (accessToken.Contains("."))
//            {
//                return await VerifyJwtAccessToken(accessToken, issuer);
//            }
//            else
//            {
//                throw new Exception("Unknown token type. Use JWT tokens!");
//            }
//        }

//        #region Helper

//        static private ConcurrentDictionary<string, DiscoveryCache> _discoveryCache;

//        async static private Task<DiscoveryDocumentResponse> GetDiscoveryDocument(string issuer)
//        {
//            if (_discoveryCache == null)
//            {
//                _discoveryCache = new ConcurrentDictionary<string, DiscoveryCache>();
//            }
//            if(!_discoveryCache.ContainsKey(issuer))
//            {
//                _discoveryCache[issuer] = new DiscoveryCache(issuer);
//            }

//            var disco = await _discoveryCache[issuer].GetAsync();
//            if (disco.IsError)
//            {
//                throw new Exception($"GetDiscoveryDocument: { disco.Error }");
//            }

//            return disco;
//        }

//        async static private Task<bool> VerifyJwtAccessToken(string token, string issuer)
//        {
//            string[] tokenParts = token.Split('.');
//            if (tokenParts.Length != 3)
//            {
//                throw new TokenValidationException("Invalid jwt access token format");
//            }

//            string headerEncoded = tokenParts[0];
//            string payloadEncoded = tokenParts[1];
//            string signatureEncoded = tokenParts[2];

//            string headerJsonString = Encoding.UTF8.GetString(Base64Url.Decode(headerEncoded));
//            var header = JsonConvert.DeserializeObject<JwtHeader>(headerJsonString);

//            string payloadJsonString = Encoding.UTF8.GetString(Base64Url.Decode(payloadEncoded));
//            var payload = JsonConvert.DeserializeObject<JwtPayload>(payloadJsonString);

//            if (issuer.ToLower() != payload.Issuer.ToLower())
//            {
//                throw new WrongIssuerException();
//            }

//            if (payload.TokenNotBeforeTime > DateTime.UtcNow)
//            {
//                throw new TokenNotBeforeException();
//            }

//            if (payload.TokenExpirationTime < DateTime.UtcNow)
//            {
//                throw new TokenExpiredException();
//            }

//            switch (header.Algorithm?.ToUpper())
//            {
//                case "RS256":

//                    #region Verify RS256

//                    var disco = await GetDiscoveryDocument(issuer);
//                    var secKey = disco.KeySet.Keys
//                        .Where(k => k.Kid == header.KeyId)
//                        .FirstOrDefault();
//                    if (secKey == null)
//                    {
//                        throw new TokenValidationException($"Can't find RS256 Key: { header.KeyId }");
//                    }

//                    RSAParameters rsaParameters = new RSAParameters();
//                    rsaParameters.Modulus = Base64Url.Decode(secKey.N);
//                    rsaParameters.Exponent = Base64Url.Decode(secKey.E);
//                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
//                    rsa.ImportParameters(rsaParameters);

//                    SHA256 sha256 = SHA256.Create();
//                    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{ headerEncoded }.{ payloadEncoded }"));

//                    RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
//                    rsaDeformatter.SetHashAlgorithm("SHA256");
//                    if (!rsaDeformatter.VerifySignature(hash, Base64Url.Decode(signatureEncoded)))
//                    {
//                        throw new InvalidSignatureException();
//                    }

//                    #endregion

//                    break;
//                default:
//                    throw new TokenValidationException($"Unsupported token algorithm { header.Algorithm }");

//            }

//            return true;
//        }

//        #endregion

//        #region Helper Classes

//        public class JwtHeader
//        {
//            [JsonProperty("alg")]
//            public string Algorithm { get; set; }

//            [JsonProperty("kid")]
//            public string KeyId { get; set; }

//            [JsonProperty("typ")]
//            public string Type { get; set; }
//        }

//        public class JwtPayload
//        {
//            #region Jwt Properties

//            [JsonProperty("nbf")]
//            public int NotBeforeTime { get; set; }

//            [JsonProperty("exp")]
//            public int ExpirationTime { get; set; }

//            [JsonProperty("iss")]
//            public string Issuer { get; set; }

//            [JsonProperty("aud")]
//            public object Audience { get; set; }  // can be string or string[] !!!

//            [JsonProperty("client_id")]
//            public string ClientId { get; set; }

//            [JsonProperty("scope")]
//            public object Scope { get; set; }  // can be string or string[] !!!

//            #endregion

//            public DateTime TokenExpirationTime
//            {
//                get
//                {
//                    return new DateTime(DateTimeOffset.FromUnixTimeSeconds(this.ExpirationTime).Ticks, DateTimeKind.Utc);
//                }
//            }

//            public DateTime TokenNotBeforeTime
//            {
//                get
//                {
//                    return new DateTime(DateTimeOffset.FromUnixTimeSeconds(this.NotBeforeTime).Ticks, DateTimeKind.Utc);
//                }
//            }
//        }

//        #endregion
//    }
//}
