using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class EditIdentityResourcePageModel : AdminPageModel, IEditIdentityResourcePageModel
    {
        public EditIdentityResourcePageModel(IResourceDbContext resourceDbContext)
        {
            _resourceDb = resourceDbContext as IResourceDbContextModify;
        }

        #region IEditIdentityResourcePageModel

        public IdentityResource CurrentIdentityResource { get; set; }

        #endregion

        async public Task LoadCurrentIdentityResourceAsync(string id)
        {
            this.CurrentIdentityResource = await _resourceDb.FindIdentityResource(id);
        }

        protected IResourceDbContextModify _resourceDb = null;
    }
}
