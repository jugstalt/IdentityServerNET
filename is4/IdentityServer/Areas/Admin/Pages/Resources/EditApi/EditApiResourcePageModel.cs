using IdentityServer.Legacy.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class EditApiResourcePageModel : PageModel, IEditApiResourcePageModel
    {
        public EditApiResourcePageModel(IResourceDbContext resourceDbContext)
        {
            _resourceDb = resourceDbContext as IResourceDbContextModify;
        }

        #region IEditApiResourceModel

        public ApiResource CurrentApiResource { get; set; }

        #endregion

        async public Task LoadCurrentApiResourceAsync(string id)
        {
            this.CurrentApiResource = await _resourceDb.FindApiResourceAsync(id);
        }

        protected IResourceDbContextModify _resourceDb = null;
    }
}
