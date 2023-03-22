using IdentityServer.Legacy.Models.IdentityServerWrappers;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public interface IEditIdentityResourcePageModel
    {
        public IdentityResourceModel CurrentIdentityResource { get; set; }
    }
}
