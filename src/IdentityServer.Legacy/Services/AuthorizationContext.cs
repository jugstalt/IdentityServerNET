using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services
{
    public class AuthorizationContext
    {
        public string ReturnUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
    }
}
