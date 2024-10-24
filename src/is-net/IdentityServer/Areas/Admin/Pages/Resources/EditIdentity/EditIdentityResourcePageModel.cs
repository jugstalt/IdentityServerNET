using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.IdentityServerWrappers;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity;

public class EditIdentityResourcePageModel : AdminPageModel, IEditIdentityResourcePageModel
{
    public EditIdentityResourcePageModel(IResourceDbContext resourceDbContext)
    {
        _resourceDb = resourceDbContext as IResourceDbContextModify;
    }

    #region IEditIdentityResourcePageModel

    public IdentityResourceModel CurrentIdentityResource { get; set; }

    #endregion

    async public Task LoadCurrentIdentityResourceAsync(string id)
    {
        this.CurrentIdentityResource = await _resourceDb.FindIdentityResource(id);
    }

    protected IResourceDbContextModify _resourceDb = null;
}
