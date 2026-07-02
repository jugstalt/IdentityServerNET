using IdentityServerNET.Models;

namespace IdentityServer.Areas.Admin.Pages.Realms.EditRealm;

public interface IEditRealmPageModel
{
    RealmModel CurrentRealm { get; set; }
}
