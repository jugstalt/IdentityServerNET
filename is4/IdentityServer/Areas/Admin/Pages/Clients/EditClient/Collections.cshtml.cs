using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

public class CollectionsModel : EditClientPageModel
{
    public CollectionsModel(IClientDbContext clientDbContext)
        : base(clientDbContext)
    {
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await LoadCurrentClientAsync(id);

        this.Input = new InputModel()
        {
            ClientId = this.CurrentClient.ClientId
        };

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentClientAsync(Input.ClientId);

            string[] values = Input.PropertyValue == null ?
                               new string[0] :
                               Input.PropertyValue
                                   .Replace("\r", "")
                                   .Split('\n')
                                   .Select(v => v.Trim())
                                   .Where(v => !String.IsNullOrEmpty(v))
                                   .ToArray();

            var propertyInfo = typeof(ClientModel).GetProperty(Input.PropertyName);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(this.CurrentClient, values);
                await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { Input.PropertyName });
            }
        }
        , onFinally: () => RedirectToPage(new { id = Input.ClientId })
        , successMessage: $"{Input.PropertyName} successfully updated");
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        public string ClientId { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string[] IgnoreProperties => new string[] { "AllowedGrantTypes", "AllowedScopes" };
    }
}
