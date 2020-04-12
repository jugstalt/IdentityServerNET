using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        async public Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentApiResourceAsync(Input.ApiName);

                string scopeName = Input.Scope?.Name?.Trim().ToLower();

                if (!String.IsNullOrWhiteSpace(scopeName))
                {
                    Input.Scope.Name = scopeName;

                    List<Scope> apiScopes = new List<Scope>();
                    if (this.CurrentApiResource.Scopes != null)
                    {
                        apiScopes.AddRange(this.CurrentApiResource.Scopes);
                    }


                    if (CurrentApiResource.Scopes == null)
                    {
                        CurrentApiResource.Scopes = new Scope[]
                        {
                        Input.Scope
                        };
                    }
                    else if (CurrentApiResource.Scopes.Where(s => s.Name == Input.Scope.Name).Count() == 0)
                    {
                        // Insert new
                        List<Scope> scopes = new List<Scope>(CurrentApiResource.Scopes);
                        scopes.Add(Input.Scope);

                        CurrentApiResource.Scopes = scopes.ToArray();
                    }
                    else
                    {
                        // Replace
                        List<Scope> scopes = new List<Scope>(CurrentApiResource.Scopes
                            .Where(s => s.Name != Input.Scope.Name && !String.IsNullOrWhiteSpace(s.Name)));
                        scopes.Add(Input.Scope);

                        CurrentApiResource.Scopes = scopes.ToArray();
                    }
                }

                await _resourceDb.UpdateApiResourceAsync(CurrentApiResource);

                return RedirectToPage(new { id = Input.ApiName });
            }, onException: (ex) => RedirectToPage(new { id = Input.ApiName }));
        }

        [BindProperty]
        public NewScopeModel Input { get; set; }

        public class NewScopeModel
        {
            public string ApiName { get; set; }
            public Scope Scope { get; set; }
        }
    }
}
