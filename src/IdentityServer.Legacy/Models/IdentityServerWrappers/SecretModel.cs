﻿using IdentityServer4.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Models.IdentityServerWrappers
{
    public class SecretModel
    {
        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Value")]

        public string Value { get; set; }

        [JsonProperty("Expiration")]
        public DateTime? Expiration { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonIgnore]
        public Secret IdentityServer4Instance
        {
            get
            {
                return new Secret()
                {
                    Description = Description,
                    Value = Value,
                    Expiration = Expiration,
                    Type = Type
                };
            }
        }
    }
}
