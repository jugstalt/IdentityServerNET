using Newtonsoft.Json;

namespace IdentityServer.Legacy.Clients
{
    public class SigningApiResponse
    {
        [JsonProperty("success")]
        public bool Succeded { get; set; }

        [JsonProperty("token")]
        public string SecurityToken { get; set; }

        [JsonProperty("errorMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorMessage { get; set; }
    }
}
