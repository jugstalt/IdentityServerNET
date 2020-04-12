using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public interface IEditIdentityResourcePageModel
    {
        public IdentityResource CurrentIdentityResource { get; set; }
    }
}
