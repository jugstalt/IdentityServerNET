#nullable enable

using IdentityServer.Net.Services;
using IdentityServerNET.Abstractions.DbContext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

public class IndexModel : EditClientPageModel
{
    public IndexModel(IClientDbContext clientDbContext)
         : base(clientDbContext)
    {
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await LoadCurrentClientAsync(id);

        Input = new InputModel()
        {
            ClientId = CurrentClient.ClientId,
            ClientName = CurrentClient.ClientName,
            ClientDescription = CurrentClient.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input is null) return RedirectToPage();

        return await SecureHandlerAsync(async () =>
        {

            await LoadCurrentClientAsync(Input.ClientId);

            CurrentClient.ClientName = Input.ClientName ?? "";
            CurrentClient.Description = Input.ClientDescription ?? "";

            if (Input.LogoFile is { Length: > 0 })
            {
                var (valid, error, bytes) = await ImageUploadValidator.ValidateAsync(Input.LogoFile);
                if (!valid) throw new IdentityServerNET.Exceptions.StatusMessageException(error);

                CurrentClient.LogoBase64 = Convert.ToBase64String(bytes!);
                CurrentClient.LogoMimeType = Input.LogoFile.ContentType;
            }

            await _clientDb.UpdateClientAsync(CurrentClient, new[] { "ClientName", "Description", "LogoBase64", "LogoMimeType" });
        }
        , onFinally: () => RedirectToPage(new { id = Input!.ClientId })
        , successMessage: "The client has been updated successfully");
    }

    public async Task<IActionResult> OnPostRemoveLogoAsync()
    {
        if (Input is null) return RedirectToPage();

        return await SecureHandlerAsync(async () =>
        {

            await LoadCurrentClientAsync(Input.ClientId);
            CurrentClient.LogoBase64 = null;
            CurrentClient.LogoMimeType = null;

            await _clientDb.UpdateClientAsync(CurrentClient, new[] { "LogoBase64", "LogoMimeType" });
        }
        , onFinally: () => RedirectToPage(new { id = Input!.ClientId })
        , successMessage: "Logo removed successfully");
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    public class InputModel
    {
        public string ClientId { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string ClientDescription { get; set; } = "";
        public IFormFile? LogoFile { get; set; }
    }
}
