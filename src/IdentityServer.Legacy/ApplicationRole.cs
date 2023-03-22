using Microsoft.AspNetCore.Identity;
using System;

namespace IdentityServer.Legacy
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
