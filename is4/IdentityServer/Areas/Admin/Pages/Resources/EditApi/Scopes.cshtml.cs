using IdentityServer.Legacy.Exceptions;
using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class ScopesModel : EditApiResourcePageModel
    {
        public ScopesModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string scopeName = "")
        {
            await LoadCurrentApiResourceAsync(id);

            this.Input = new NewScopeModel()
            {
                ApiName = CurrentApiResource.Name,
                Scope = String.IsNullOrWhiteSpace(scopeName) ?
                            null :
                            CurrentApiResource.Scopes.Where(s => s.Name == scopeName).FirstOrDefault()
            };

            return Page();
        }

        async public Task<IActionResult> OnGetRemoveAsync(string id, string scopeName)
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentApiResourceAsync(id);

                if (this.CurrentApiResource.Scopes != null &&
                    this.CurrentApiResource.Scopes.Where(s => s.Name == scopeName).Count() >= 0)
                {
                    this.CurrentApiResource.Scopes = this.CurrentApiResource.Scopes
                                                            .Where(s => s.Name != scopeName)
                                                            .ToArray();

                    await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource, new[] { "Scopes" });
                }
            }
            , onFinally: () => RedirectToPage(new { id = id })
            , successMessage: $"Successfully removed scope '{ scopeName }'");

        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentApiResourceAsync(Input.ApiName);

                bool checkNameConvention = true;
                if (Input.Scope.Name != null && Input.Scope.Name.StartsWith("@@"))
                {
                    Input.Scope.Name = Input.Scope.Name.Substring(2);
                    checkNameConvention = false;
                }

                if (String.IsNullOrWhiteSpace(Input.Scope?.Name) ||
                   Input.Scope.Name.Trim().Length<3)
                {
                    throw new StatusMessageException("Invalid scope name: min. 3 letters, mumbers, . - _");
                }

                var regEx = new Regex(@"^[a-z0-9_\-\.]+$");
                if(!regEx.IsMatch(Input.Scope.Name))
                {
                    throw new StatusMessageException("Invalid scope name: Only lowercase letters, numbers,-,_,.");
                }

                if (checkNameConvention)
                {
                    if (Input.Scope.Name != this.CurrentApiResource.Name &&
                       !Input.Scope.Name.StartsWith(this.CurrentApiResource.Name + "."))
                    {
                        throw new StatusMessageException($"Bad name convention: Scope names for this API resource shold start with '{ CurrentApiResource.Name }.'. If you want to overrule this convonention, type @@ befor your scope name...");
                    }
                }

                string scopeName = Input.Scope?.Name?.Trim().ToLower();

                if (!String.IsNullOrWhiteSpace(scopeName))
                {
                    Input.Scope.Name = scopeName;

                    List<ScopeModel> apiScopes = new List<ScopeModel>();
                    if (this.CurrentApiResource.Scopes != null)
                    {
                        apiScopes.AddRange(this.CurrentApiResource.Scopes);
                    }


                    if (CurrentApiResource.Scopes == null)
                    {
                        CurrentApiResource.Scopes = new ScopeModel[]
                        {
                            Input.Scope
                        };
                    }
                    else if (CurrentApiResource.Scopes.Where(s => s.Name == Input.Scope.Name).Count() == 0)
                    {
                        // Insert new
                        List<ScopeModel> scopes = new List<ScopeModel>(CurrentApiResource.Scopes);
                        scopes.Add(Input.Scope);

                        CurrentApiResource.Scopes = scopes.ToArray();
                    }
                    else
                    {
                        // Replace
                        List<ScopeModel> scopes = new List<ScopeModel>(CurrentApiResource.Scopes
                            .Where(s => s.Name != Input.Scope.Name && !String.IsNullOrWhiteSpace(s.Name)));
                        scopes.Add(Input.Scope);

                        CurrentApiResource.Scopes = scopes.ToArray();
                    }
                }

                await _resourceDb.UpdateApiResourceAsync(CurrentApiResource, new[] { "Scopes" });
            }
            , onFinally: () => RedirectToPage(new { id = Input.ApiName })
            , successMessage: "Scopes successfully updated"
            , onException: (ex) => RedirectToPage(new { id = Input.ApiName }));
        }

        [BindProperty]
        public NewScopeModel Input { get; set; }

        public class NewScopeModel
        {
            public string ApiName { get; set; }

            public ScopeModel Scope { get; set; }
        }
    }
}
