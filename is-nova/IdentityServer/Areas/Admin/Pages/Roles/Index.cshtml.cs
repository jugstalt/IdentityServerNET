using IdentityServer.Nova;
using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Roles;

public class IndexModel : SecurePageModel
{
    private IRoleDbContext _roleDb = null;

    public IndexModel(IRoleDbContext roleDbContext)
    {
        _roleDb = roleDbContext;
    }

    public IEnumerable<ApplicationRole> ApplicationRoles { get; set; }

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
        [RegularExpression(@"^[a-z0-9\-_]*$", ErrorMessage = "Only lowercase letters, numbers, -, _ is allowed")]
        [MinLength(3)]
        public string Rolename { get; set; }
        public string Description { get; set; }
    }

    async public Task<IActionResult> OnGetAsync(int skip = 0)
    {
        if (_roleDb is IAdminRoleDbContext)
        {
            this.ApplicationRoles = await ((IAdminRoleDbContext)_roleDb).GetRolesAsync(100, skip, CancellationToken.None);
        }

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        string roleId = String.Empty;

        return await SecureHandlerAsync(async () =>
        {
            if (!ModelState.IsValid)
            {
                throw new StatusMessageException($"Type a valid role name.");
            }

            var role = new ApplicationRole() { Id = CreateInput.Rolename, Name = CreateInput.Rolename, Description = CreateInput.Description };
            await _roleDb.CreateAsync(role, CancellationToken.None);

            roleId = role.Id;

        },
        onFinally: () => RedirectToPage("EditRole/Index", new { id = roleId }),
        successMessage: "",
        onException: (ex) => RedirectToPage());
    }

    async public Task<IActionResult> OnGetAddKnownRoleAsync(string name)
    {
        string roleId = String.Empty;

        return await SecureHandlerAsync(async () =>
        {
            var knownRole = typeof(KnownRoles)
                                .GetMethods()
                                .Where(m => m.ReturnType == typeof(ApplicationRole))
                                .Select(m => (ApplicationRole)m.Invoke(Activator.CreateInstance<KnownRoles>(), null))
                                .Where(r => r.Name == name)
                                .FirstOrDefault();

            if (knownRole == null)
            {
                throw new StatusMessageException($"Unknown role {name}");
            }

            var existsingRole = await _roleDb.FindByNameAsync(knownRole.Name, CancellationToken.None);
            if (existsingRole != null)
            {
                roleId = existsingRole.Id;
            }
            else
            {
                await _roleDb.CreateAsync(knownRole, CancellationToken.None);
                roleId = knownRole.Id;
            }
        },
        onFinally: () => RedirectToPage("EditRole/Index", new { id = roleId }),
        successMessage: "",
        onException: (ex) => RedirectToPage());
    }

    async public Task<IActionResult> OnPostFilterAsync()
    {
        if (String.IsNullOrWhiteSpace(Filter.Term))
        {
            Filter.Term = "";
            return await OnGetAsync();
        }

        return await SecureHandlerAsync(async () =>
        {
            this.ApplicationRoles = _roleDb switch
            {
                IAdminRoleDbContext adminUserDb =>
                       await adminUserDb.FindRoles(Filter.Term, CancellationToken.None) ?? [],
                _ => [await _roleDb.FindByNameAsync(Filter.Term?.ToString(), CancellationToken.None)]
            };

            this.ApplicationRoles = this.ApplicationRoles.Where(u => u != null).ToArray();
            if (this.ApplicationRoles?.Any() == false)
            {
                throw new StatusMessageException($"{Filter.Term} do not match any role");
            }
        },
        onFinally: () => Page(), //RedirectToPage("EditRole/Index", new { id = roleId }),
        successMessage: "",
        onException: (ex) => RedirectToPage());
    }
}
