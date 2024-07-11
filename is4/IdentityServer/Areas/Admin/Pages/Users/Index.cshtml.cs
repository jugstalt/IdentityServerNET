using IdentityServer.Nova;
using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users;

public class IndexModel : SecurePageModel
{
    private IPasswordHasher<ApplicationUser> _passwordHasher = null;
    private IUserDbContext _userDb = null;

    public IndexModel(
        IPasswordHasher<ApplicationUser> passwordHasher,
        IUserDbContext userDbContext)
    {
        _passwordHasher = passwordHasher;
        _userDb = userDbContext;
    }

    public IEnumerable<ApplicationUser> ApplicationUsers { get; set; }

    [BindProperty]
    public FilterModel Filter { get; set; }

    public class FilterModel
    {
        public string Term { get; set; }
    }

    [BindProperty]
    public CreateInputModel CreateInput { get; set; }

    public class CreateInputModel
    {
        [Required]
        [RegularExpression(@"^[a-z0-9\-_@\.]*$", ErrorMessage = "Only lowercase letters, numbers, [- _ @ .] is allowed")]
        [MinLength(5)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }

    async public Task<IActionResult> OnGetAsync(int skip = 0)
    {
        if (_userDb is IAdminUserDbContext)
        {
            this.ApplicationUsers = await ((IAdminUserDbContext)_userDb).GetUsersAsync(100, skip, CancellationToken.None);
        }

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        string userId = String.Empty;

        return await SecureHandlerAsync(async () =>
        {
            if (!ModelState.IsValid)
            {
                throw new StatusMessageException($"Type a valid username.");
            }

            var user = new ApplicationUser()
            {
                Id = Guid.NewGuid().ToString(),
                UserName = CreateInput.Username
            };

            if ((await _userDb.FindByNameAsync(user.UserName, CancellationToken.None)) != null)
            {
                throw new StatusMessageException("User already exisits");
            }

            if (Regex.Match(user.UserName, ValidationExtensions.EmailAddressRegex).Success == true)
            {
                user.Email = user.UserName;
                user.EmailConfirmed = true;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, CreateInput.Password);

            var result = await _userDb.CreateAsync(user, CancellationToken.None);
            if (result.Succeeded == false)
            {
                throw new StatusMessageException($"Can't create user: {result.Errors?.FirstOrDefault()?.Description}");
            }

            user = await _userDb.FindByNameAsync(user.UserName, CancellationToken.None);
            userId = user.Id;
        },
        onFinally: () => RedirectToPage("EditUser/Index", new { id = userId }),
        successMessage: "",
        onException: (ex) => RedirectToPage());
    }

    async public Task<IActionResult> OnPostFindAsync()
    {
        //string userId = String.Empty;

        if (String.IsNullOrWhiteSpace(Filter.Term))
        {
            Filter.Term = "";
            return await OnGetAsync();
        }

        return await SecureHandlerAsync(async () =>
            {
                this.ApplicationUsers = _userDb switch
                {
                    IAdminUserDbContext adminUserDb =>
                            await adminUserDb.FindUsers(Filter.Term, CancellationToken.None) ?? [],
                    _ => [await _userDb.FindByNameAsync(Filter.Term?.ToString(), CancellationToken.None)]
                };

                this.ApplicationUsers = this.ApplicationUsers.Where(u => u != null).ToArray();
                if (this.ApplicationUsers?.Any() == false)
                {
                    throw new StatusMessageException($"{Filter.Term} do not match any user");
                }
            },
            onFinally: () => Page(), // RedirectToPage("EditUser/Index", new { id = userId }),
            successMessage: "",
            onException: (ex) => RedirectToPage()
        );
    }
}
