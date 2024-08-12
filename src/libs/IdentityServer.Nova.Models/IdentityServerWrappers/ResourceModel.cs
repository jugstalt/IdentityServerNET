using IdentityServer4.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IdentityServer.Nova.Models.IdentityServerWrappers;

abstract public class ResourceModel
{
    public ResourceModel()
    {
        this.Name = String.Empty;
        this.DisplayName = String.Empty;
        this.Enabled = true;
        this.UserClaims = new List<string>();
        this.Properties = new Dictionary<string, string>();
    }

    public ResourceModel(string name, string displayName)
        : this()
    {
        this.Name = name;
        this.DisplayName = displayName;
    }

    [JsonProperty("Enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("DisplayName")]
    public string DisplayName { get; set; }

    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonProperty("UserClaims")]
    public ICollection<string> UserClaims { get; set; }

    [JsonProperty("Properties")]
    public IDictionary<string, string> Properties { get; set; }

    public abstract Resource IndentityServer4Instance { get; }
}
