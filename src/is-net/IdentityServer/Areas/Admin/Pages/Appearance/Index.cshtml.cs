#nullable enable

using IdentityServer.Net.Services;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using IdentityServerNET.Services.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Appearance;

public class IndexModel : SecurePageModel
{
    private readonly UICustomizationService _customization;

    public IndexModel(UICustomizationService customization)
    {
        _customization = customization;
    }

    public UICustomizationSettings Settings { get; private set; } = new();

    [BindProperty] public string? ApplicationTitle { get; set; }
    [BindProperty] public string? PrimaryColor { get; set; }
    [BindProperty] public string? OnPrimaryColor { get; set; }
    [BindProperty] public string? HeadingColor { get; set; }
    [BindProperty] public string? BodyTextColor { get; set; }
    [BindProperty] public IFormFile? LogoFile { get; set; }
    [BindProperty] public IReadOnlyList<IFormFile>? BackgroundFiles { get; set; }

    public async Task OnGetAsync()
    {
        Settings = await _customization.LoadAsync();
    }

    public Task<IActionResult> OnPostAsync([FromQuery] string? action)
    {
        return SecureHandlerAsync(async () =>
        {
            Settings = await _customization.LoadAsync();

            if (action == "remove-logo")
            {
                await _customization.RemoveLogoAsync();
                return;
            }

            if (action == "reset-on-primary")
            {
                Settings.OnPrimaryColor = null;
                await _customization.SaveAsync(Settings);
                return;
            }

            if (action == "reset-heading")
            {
                Settings.HeadingColor = null;
                await _customization.SaveAsync(Settings);
                return;
            }

            if (action == "reset-body-text")
            {
                Settings.BodyTextColor = null;
                await _customization.SaveAsync(Settings);
                return;
            }

            if (action == "remove-all-backgrounds")
            {
                Settings.Backgrounds = null;
                await _customization.SaveAsync(Settings);
                return;
            }

            if (action != null && action.StartsWith("remove-background-") &&
                int.TryParse(action["remove-background-".Length..], out int idx))
            {
                await _customization.RemoveBackgroundAsync(idx);
                return;
            }

            // Regular save
            Settings.ApplicationTitle = ApplicationTitle;
            Settings.PrimaryColor = PrimaryColor;
            Settings.OnPrimaryColor = string.IsNullOrEmpty(OnPrimaryColor) || OnPrimaryColor == "#000000" ? null : OnPrimaryColor;
            Settings.HeadingColor   = string.IsNullOrEmpty(HeadingColor)   || HeadingColor   == "#000000" ? null : HeadingColor;
            Settings.BodyTextColor  = string.IsNullOrEmpty(BodyTextColor)  || BodyTextColor  == "#000000" ? null : BodyTextColor;

            if (LogoFile is { Length: > 0 })
            {
                var (valid, error, bytes) = await ImageUploadValidator.ValidateAsync(LogoFile);
                if (!valid) throw new StatusMessageException(error);

                Settings.LogoBase64   = Convert.ToBase64String(bytes!);
                Settings.LogoMimeType = LogoFile.ContentType;
            }

            if (BackgroundFiles is { Count: > 0 })
            {
                Settings.Backgrounds ??= new List<UIBackgroundImage>();
                foreach (var file in BackgroundFiles)
                {
                    if (file.Length == 0) continue;

                    var (valid, error, bytes) = await ImageUploadValidator.ValidateAsync(file);
                    if (!valid) throw new StatusMessageException($"Background '{file.FileName}': {error}");

                    Settings.Backgrounds.Add(new UIBackgroundImage
                    {
                        Base64   = Convert.ToBase64String(bytes!),
                        MimeType = file.ContentType
                    });
                }
            }

            await _customization.SaveAsync(Settings);
        },
        () => RedirectToPage("./Index"),
        "Appearance saved.");
    }
}
