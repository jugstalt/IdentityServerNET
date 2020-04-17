using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser
{
    public class SetPasswordModel : EditUserPageModel
    {
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        public SetPasswordModel(
            IPasswordHasher<ApplicationUser> passwordHasher,
            IUserDbContext userDbContext,
            IOptions<UserDbContextConfiguration> userDbContextConfiguration)
            : base(userDbContext, userDbContextConfiguration)
        {
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [HiddenInput]
            public string CurrentUserId { get; set; }

            [Display(Name = "Current password hash")]
            public string CurrentPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            if (_passwordHasher == null)
            {
                return NotFound($"No passwordhasher implemented.");
            }

            await base.LoadCurrentApplicationUserAsync(id);
            if (this.CurrentApplicationUser == null)
            {
                return NotFound($"Unable to load user.");
            }

            this.Input = new InputModel()
            {
                CurrentUserId = this.CurrentApplicationUser.Id,
                CurrentPassword = this.CurrentApplicationUser.PasswordHash.Substring(0, Math.Min(20, this.CurrentApplicationUser.PasswordHash.Length)) + "..."
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await base.LoadCurrentApplicationUserAsync(Input.CurrentUserId);

            #region Verify new Password

            try
            {
                Input.NewPassword = Input.NewPassword.Trim();

                if (String.IsNullOrWhiteSpace(Input.NewPassword))
                {
                    throw new Exception("New password is empty.");
                }

                if(!Input.NewPassword.Equals(Input.ConfirmPassword))
                {
                    throw new Exception("The two passwords don't match.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return RedirectToPage(new { id = Input.CurrentUserId });
            }

            #endregion

            string newPasswordhash = _passwordHasher.HashPassword(this.CurrentApplicationUser, Input.NewPassword);

            await _userDbContext.UpdatePropertyAsync<string>(this.CurrentApplicationUser, "PasswordHash", newPasswordhash, CancellationToken.None);

            return RedirectToPage(new { id = Input.CurrentUserId });
        }
    }
}
