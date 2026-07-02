using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Realms.EditRealm;

public class EditRealmPageModel : SecurePageModel, IEditRealmPageModel
{
    protected readonly IRealmDbContext _realmDb;

    protected EditRealmPageModel(IRealmDbContext realmDb)
    {
        _realmDb = realmDb;
    }

    public RealmModel CurrentRealm { get; set; }

    protected async Task LoadCurrentRealmAsync(string name)
    {
        CurrentRealm = await _realmDb.FindByNameAsync(name, CancellationToken.None);
    }
}
