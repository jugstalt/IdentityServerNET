using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.IdentityServerWrappers;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi;

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
        var apiResource = await _resourceDb.FindApiResourceAsync(id);
        this.CurrentApiResource = apiResource != null && await this.IsInCurrentRealmAsync(apiResource.Name)
            ? apiResource
            : null;
    }

    protected IResourceDbContextModify _resourceDb = null;
}
