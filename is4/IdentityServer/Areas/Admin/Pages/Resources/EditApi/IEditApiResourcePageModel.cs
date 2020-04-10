using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public interface IEditApiResourcePageModel
    {
        public ApiResource CurrentApiResource { get; set; }
    }
}
