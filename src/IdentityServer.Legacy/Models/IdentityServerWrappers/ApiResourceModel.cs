using IdentityServer4.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer.Legacy.Models.IdentityServerWrappers
{


    public class ApiResourceModel : ResourceModel
    {
        public ApiResourceModel()
            : base()
        { }

        public ApiResourceModel(string name, string displayName)
            : base(name, displayName)
        {

        }

        [JsonProperty("ApiSecrets")]
        public ICollection<SecretModel> ApiSecrets { get; set; }

        [JsonProperty("Scopes")]
        public ICollection<ScopeModel> Scopes { get; set; }

        [JsonIgnore]
        public override Resource IndentityServer4Instance
        {
            get
            {
                var apiResource = new ApiResource(this.Name, this.DisplayName);

                apiResource.Enabled = this.Enabled;
                apiResource.Description = this.Description;
                apiResource.UserClaims = this.UserClaims;
                apiResource.Properties = this.Properties;

                apiResource.ApiSecrets = this.ApiSecrets?.Select(s => s.IdentityServer4Instance).ToList();
                apiResource.Scopes = this.Scopes?.Select(s => s.IdentitityServer4Insance.Name).ToList();

                return apiResource;
            }
        }
    }
}
