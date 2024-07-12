using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace IdentityServer.Nova.Models;

public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        // Test
        //Claims = new Claim[]
        //    {
        //        new Claim(JwtClaimTypes.Name, "Alice Smith"),
        //        new Claim(JwtClaimTypes.GivenName, "Alice"),
        //        new Claim(JwtClaimTypes.FamilyName, "Smith"),
        //        new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
        //        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
        //        new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
        //        new Claim(JwtClaimTypes.Role, "rol1,role2"),
        //        new Claim(JwtClaimTypes.Address, @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", ClaimValueTypes.String)
        //    };
    }

    public bool IsLocked { get; set; }

    #region Claims

    private ICollection<Claim>? _claims = null;

    [JsonIgnore]
    public ICollection<Claim> Claims
    {
        get
        {
            if (this._claims == null)
            {
                return new Claim[0];
            }

            return this._claims;
        }

        set
        {
            _claims = value;
        }
    }

    [JsonProperty("Claims")]
    public SerializableClaim[] SerializableClaims
    {
        get
        {
            return this.Claims.Select(c => new SerializableClaim()
            {
                Type = c.Type,
                Value = c.Value,
                ValueType = c.ValueType
            }).ToArray();
        }
        set
        {
            if (value != null)
            {
                _claims = value.Select(c => new Claim(c.Type, c.Value, c.Value)).ToArray();
            }
        }
    }

    #endregion

    public ICollection<string>? Roles { get; set; }

    #region TFA

    public string AuthenticatorKey { get; set; } = "";
    public IEnumerable<string>? TfaRecoveryCodes { get; set; }

    #endregion

    #region Classes

    public class SerializableClaim
    {
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
        public string ValueType { get; set; } = "";
    }

    #endregion
}
