using IdentityServer4.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Models.IdentityServerWrappers
{
    public class ScopeModel
    {
        public ScopeModel()
        {
            this.Name = String.Empty;
            this.DisplayName = String.Empty;
            this.UserClaims = new List<string>();
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Required")]
        public bool Required { get; set; }

        [JsonProperty("Emphasize")]
        public bool Emphasize { get; set; }

        [JsonProperty("ShowInDiscoveryDocument")]
        public bool ShowInDiscoveryDocument { get; set; }

        [JsonProperty("UserClaims")]
        public ICollection<string> UserClaims { get; set; }

        [JsonIgnore]
        public Scope IdentitityServer4Insance
        {
            get
            {
                return new Scope()
                {
                    Name = Name,
                    DisplayName = DisplayName,
                    Description = Description,
                    Required = Required,
                    Emphasize = Emphasize,
                    ShowInDiscoveryDocument = ShowInDiscoveryDocument,
                    UserClaims = UserClaims
                };
            }
        }
    }
}
