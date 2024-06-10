using Microsoft.AspNetCore.Identity;
using System;

namespace IdentityServer.Nova
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
