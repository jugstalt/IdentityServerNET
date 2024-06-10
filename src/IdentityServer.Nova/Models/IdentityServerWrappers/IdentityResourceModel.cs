using IdentityServer4.Models;
using Newtonsoft.Json;

namespace IdentityServer.Nova.Models.IdentityServerWrappers
{
    public class IdentityResourceModel : ResourceModel
    {
        public IdentityResourceModel()
            : base()
        { }

        public IdentityResourceModel(string name, string displayName)
            : base(name, displayName)
        {

        }

        public IdentityResourceModel(IdentityResource identityResource)
        {
            this.Name = identityResource.Name;
            this.DisplayName = identityResource.DisplayName;
            this.Description = identityResource.Description;
            this.Enabled = identityResource.Enabled;

            this.UserClaims = identityResource.UserClaims;
            this.Properties = identityResource.Properties;

            this.Required = identityResource.Required;
            this.Emphasize = identityResource.Emphasize;
            this.ShowInDiscoveryDocument = identityResource.ShowInDiscoveryDocument;
        }

        [JsonProperty("Required")]
        public bool Required { get; set; }

        [JsonProperty("Emphasize")]
        public bool Emphasize { get; set; }

        [JsonProperty("ShowInDiscoveryDocument")]
        public bool ShowInDiscoveryDocument { get; set; }

        [JsonIgnore]
        public override Resource IndentityServer4Instance
        {
            get
            {
                var identityResource = new IdentityResource();

                identityResource.Name = this.Name;
                identityResource.DisplayName = this.DisplayName;
                identityResource.Enabled = this.Enabled;
                identityResource.Description = this.Description;
                identityResource.UserClaims = this.UserClaims;
                identityResource.Properties = this.Properties;

                identityResource.Required = this.Required;
                identityResource.Emphasize = this.Emphasize;
                identityResource.ShowInDiscoveryDocument = this.ShowInDiscoveryDocument;

                return identityResource;
            }
        }
    }
}
