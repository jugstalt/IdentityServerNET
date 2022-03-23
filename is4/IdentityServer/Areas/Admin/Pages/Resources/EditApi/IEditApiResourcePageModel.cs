using IdentityServer.Legacy.Models;
using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public interface IEditApiResourcePageModel
    {
        public ApiResourceModel CurrentApiResource { get; set; }
    }
}
