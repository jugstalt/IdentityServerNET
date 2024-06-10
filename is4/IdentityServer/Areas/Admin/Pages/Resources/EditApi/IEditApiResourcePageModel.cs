using IdentityServer.Nova.Models.IdentityServerWrappers;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public interface IEditApiResourcePageModel
    {
        public ApiResourceModel CurrentApiResource { get; set; }
    }
}
