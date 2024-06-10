using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.DbContext;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class EditApiResourcePageModel : AdminPageModel, IEditApiResourcePageModel
    {
        public EditApiResourcePageModel(IResourceDbContext resourceDbContext)
        {
            _resourceDb = resourceDbContext as IResourceDbContextModify;
        }

        #region IEditApiResourceModel

        public ApiResourceModel CurrentApiResource { get; set; }

        #endregion

        async public Task LoadCurrentApiResourceAsync(string id)
        {
            this.CurrentApiResource = await _resourceDb.FindApiResourceAsync(id);
        }

        protected IResourceDbContextModify _resourceDb = null;
    }
}
