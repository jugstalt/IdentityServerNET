using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
