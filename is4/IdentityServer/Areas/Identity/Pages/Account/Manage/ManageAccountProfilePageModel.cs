using IdentityServer.Nova;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.UserInteraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public class ManageAccountProfilePageModel : PageModel, IManageAccountPageModel
    {
        protected IUserStoreFactory _userStoreFactory;

        protected ManageAccountProfilePageModel(IUserStoreFactory userStoreFactory)
        {
            _userStoreFactory = userStoreFactory;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public ApplicationUser ApplicationUser { get; set; }

        public string Category { get; set; }

        async public Task<ManageAccountEditor> EditorInfos() => (await _userStoreFactory.CreateUserDbContextInstance())?.ContextConfiguration?.ManageAccountEditor;

        async protected Task<IUserDbContext> CurrentUserDbContext() => await _userStoreFactory.CreateUserDbContextInstance();

        async protected Task LoadUserAsync()
        {
            var userDbContext = await _userStoreFactory.CreateUserDbContextInstance();
            this.ApplicationUser = await userDbContext.FindByNameAsync(User.Identity?.Name, new System.Threading.CancellationToken());
        }
    }
}
