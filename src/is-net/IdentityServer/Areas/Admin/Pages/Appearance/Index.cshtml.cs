#nullable enable

using IdentityServer.Net.Services;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using IdentityServerNET.Services.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Appearance;

public class IndexModel : SecurePageModel
{
    private readonly UICustomizationService _customization;
    private readonly IRealmContext? _realmContext;
    private readonly IRealmDbContext? _realmDb;

    public IndexModel(
        UICustomizationService customization,
        IRealmContext? realmContext = null,
        IRealmDbContext? realmDb = null)
    {
        _customization = customization;
        _realmContext = realmContext;
        _realmDb = realmDb;
    }

    public UICustomizationSettings Settings { get; private set; } = new();

    /// <summary>Current realm name for realm admins; null for system admin.</summary>
    public string? CurrentRealm { get; private set; }

    [BindProperty] public string? ApplicationTitle { get; set; }
    [BindProperty] public string? PrimaryColor { get; set; }
    [BindProperty] public string? OnPrimaryColor { get; set; }
    [BindProperty] public string? HeadingColor { get; set; }
    [BindProperty] public string? BodyTextColor { get; set; }
    [BindProperty] public IFormFile? LogoFile { get; set; }
    [BindProperty] public IReadOnlyList<IFormFile>? BackgroundFiles { get; set; }

    public async Task OnGetAsync()
    {
        CurrentRealm = await GetCurrentRealmAsync();
        Settings = await LoadSettingsAsync(CurrentRealm);
    }

    public Task<IActionResult> OnPostAsync([FromQuery] string? action)
    {
        return SecureHandlerAsync(async () =>
        {
            CurrentRealm = await GetCurrentRealmAsync();
            Settings = await LoadSettingsAsync(CurrentRealm);

            if (action == "remove-logo")
            {
                Settings.LogoBase64   = null;
                Settings.LogoMimeType = null;
                await SaveSettingsAsync(Settings, CurrentRealm);
                return;
            }

            if (action == "reset-on-primary")
            {
                Settings.OnPrimaryColor = null;
                await SaveSettingsAsync(Settings, CurrentRealm);
                return;
            }

            if (action == "reset-heading")
            {
                Settings.HeadingColor = null;
                await SaveSettingsAsync(Settings, CurrentRealm);
                return;
            }

            if (action == "reset-body-text")
            {
                Settings.BodyTextColor = null;
                await SaveSettingsAsync(Settings, CurrentRealm);
                return;
            }

            if (action == "remove-all-backgrounds")
            {
                Settings.Backgrounds = null;
                await SaveSettingsAsync(Settings, CurrentRealm);
                return;
            }

            if (action != null && action.StartsWith("remove-background-") &&
                int.TryParse(action["remove-background-".Length..], out int idx))
            {
                Settings.Backgrounds?.RemoveAt(idx - 1);
                await SaveSettingsAsync(Settings, CurrentRealm);
                return;
            }

            // Regular save
            Settings.ApplicationTitle = ApplicationTitle;
            Settings.PrimaryColor     = PrimaryColor;
            Settings.OnPrimaryColor   = string.IsNullOrEmpty(OnPrimaryColor) || OnPrimaryColor == "#000000" ? null : OnPrimaryColor;
            Settings.HeadingColor     = string.IsNullOrEmpty(HeadingColor)   || HeadingColor   == "#000000" ? null : HeadingColor;
            Settings.BodyTextColor    = string.IsNullOrEmpty(BodyTextColor)  || BodyTextColor  == "#000000" ? null : BodyTextColor;

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

                    var (valid, error, bytes) = await ImageUploadValidator.ValidateAsync(file, ImageUploadValidator.BackgroundMaxBytes);
                    if (!valid) throw new StatusMessageException($"Background '{file.FileName}': {error}");

                    Settings.Backgrounds.Add(new UIBackgroundImage
                    {
                        Base64   = Convert.ToBase64String(bytes!),
                        MimeType = file.ContentType
                    });
                }
            }

            await SaveSettingsAsync(Settings, CurrentRealm);
        },
        () => RedirectToPage("./Index"),
        "Appearance saved.");
    }

    // ------------------------------------------------------------------

    private async Task<string?> GetCurrentRealmAsync()
        => _realmContext is not null ? await _realmContext.GetCurrentRealmNameAsync() : null;

    private async Task<UICustomizationSettings> LoadSettingsAsync(string? realm)
    {
        if (realm is not null && _realmDb is not null)
        {
            var realmModel = await _realmDb.FindByNameAsync(realm, CancellationToken.None);
            return UICustomizationService.ToUISettings(realmModel?.Appearance);
        }
        return await _customization.LoadAsync();
    }

    private async Task SaveSettingsAsync(UICustomizationSettings settings, string? realm)
    {
        if (realm is not null && _realmDb is not null)
        {
            var realmModel = await _realmDb.FindByNameAsync(realm, CancellationToken.None);
            if (realmModel is not null)
            {
                realmModel.Appearance = UICustomizationService.FromUISettings(settings);
                await _realmDb.UpdateAsync(realmModel, CancellationToken.None);
            }
            return;
        }
        await _customization.SaveAsync(settings);
    }
}
