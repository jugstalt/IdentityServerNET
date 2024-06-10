using IdentityServer.Nova;
using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole
{
    public class DeleteRoleModel : EditRolePageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DeleteRoleModel(
            SignInManager<ApplicationUser> signInManager,
            IRoleDbContext roleDbContext,
            IUserDbContext userDbContext,
            IOptions<RoleDbContextConfiguration> roleDbContextConfiguration = null)
            : base(roleDbContext, roleDbContextConfiguration)
        {
            _signInManager = signInManager;
            _userDbContext = userDbContext;
        }

        private IUserDbContext _userDbContext;

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [HiddenInput]
            public string CurrentRoleId { get; set; }

            [Display(Name = "Confirm Rolename")]
            public string ConfirmRolename { get; set; }
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await base.LoadCurrentApplicationRoleAsync(id);
            if (this.CurrentApplicationRole == null)
            {
                return NotFound($"Unable to load role.");
            }

            this.Input = new InputModel()
            {
                CurrentRoleId = this.CurrentApplicationRole.Id,
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await base.SecureHandlerAsync(async () =>
            {
                await base.LoadCurrentApplicationRoleAsync(Input.CurrentRoleId);

                #region Verify Username

                if (!this.CurrentApplicationRole.Name.Equals(Input.ConfirmRolename))
                {
                    throw new StatusMessageException("Please type the correct username.");
                }

                #endregion

                #region Remove Role from users

                if (_userDbContext is IUserRoleDbContext)
                {
                    var userRoleDb = (IUserRoleDbContext)_userDbContext;

                    foreach (var user in await userRoleDb.GetUsersInRoleAsync(CurrentApplicationRole.Name, CancellationToken.None))
                    {
                        await userRoleDb.RemoveFromRoleAsync(user, CurrentApplicationRole.Name, CancellationToken.None);
                        await _signInManager.RefreshSignInAsync(user);
                    }
                }

                #endregion

                await _roleDbContext.DeleteAsync(this.CurrentApplicationRole, CancellationToken.None);
            }
            , onFinally: () => RedirectToPage("../Index")
            , successMessage: ""
            , onException: (ex) => RedirectToPage(new { id = Input.CurrentRoleId }));
        }
    }
}
