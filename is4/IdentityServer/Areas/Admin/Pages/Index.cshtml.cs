using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin
{
    public class IndexModel : PageModel
    {
        public IndexModel(IUserDbContext userDbContext = null, IRoleDbContext roleDbContext = null, IResourceDbContext resourceDbContxt = null, IClientDbContext clientDbContext = null)
        {
            this.HasUserDb = userDbContext != null;
            this.HasRoleDb = roleDbContext != null;
            this.HasResourceOrClientDb = resourceDbContxt != null || clientDbContext != null;
        }


        public bool HasUserDb { get; private set; }
        public bool HasRoleDb { get; private set; }
        public bool HasResourceOrClientDb { get; private set; }

        public void OnGet()
        {
        }
    }
}
