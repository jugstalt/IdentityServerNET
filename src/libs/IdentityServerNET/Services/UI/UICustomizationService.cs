using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.UI;

public class UICustomizationService
{
    private readonly IUIDbContext _db;

    // Base CSS content set once at startup from IUserInterfaceService.OverrideCssContent
    private string _baseOverrideCss = "";

    // In-memory caches — all invalidated together on save
    private UICustomizationSettings? _settings;
    private string? _overrideCss;
    private (byte[] data, string mime)? _logoBytes;
    private readonly ConcurrentDictionary<int, (byte[] data, string mime)> _bgBytes = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public UICustomizationService(IUIDbContext db)
    {
        _db = db;
    }

    // Called once at startup to hand in the app-level base CSS.
    public void Initialize(string baseOverrideCss)
    {
        _baseOverrideCss = baseOverrideCss;
    }

    // ── Load / Cache ──────────────────────────────────────────────────────────

    public async Task<UICustomizationSettings> LoadAsync()
    {
        if (_settings is not null) return _settings;

        await _loadLock.WaitAsync();
        try
        {
            return _settings ??= await _db.GetSettingsAsync() ?? new UICustomizationSettings();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    // ── Endpoint helpers ──────────────────────────────────────────────────────

    public async Task<string> GetOverrideCssAsync()
    {
        if (_overrideCss is not null) return _overrideCss;
        var settings = await LoadAsync();
        return _overrideCss = GenerateOverrideCss(settings);
    }

    public async Task<(byte[] data, string mime)?> GetLogoAsync()
    {
        if (_logoBytes is not null) return _logoBytes;
        var settings = await LoadAsync();
        if (!settings.HasLogo) return null;
        _logoBytes = (Convert.FromBase64String(settings.LogoBase64!), settings.LogoMimeType ?? "image/png");
        return _logoBytes;
    }

    public async Task<(byte[] data, string mime)?> GetBackgroundAsync(int index)
    {
        if (_bgBytes.TryGetValue(index, out var cached)) return cached;
        var settings = await LoadAsync();
        var bg = settings.Backgrounds?.ElementAtOrDefault(index - 1);
        if (bg is null || string.IsNullOrEmpty(bg.Base64)) return null;
        var result = (Convert.FromBase64String(bg.Base64), bg.MimeType ?? "image/jpeg");
        _bgBytes[index] = result;
        return result;
    }

    // Used by MailTemplateService for {{logoBase64Tag}} placeholder
    public async Task<string> GetLogoBase64TagAsync()
    {
        var logo = await GetLogoAsync();
        if (logo is null) return "";
        var b64 = Convert.ToBase64String(logo.Value.data);
        return $"""<img src="data:{logo.Value.mime};base64,{b64}" width="48" height="48" alt="Logo" style="border-radius:8px;vertical-align:middle;">""";
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    public async Task SaveAsync(UICustomizationSettings settings)
    {
        await _db.SaveSettingsAsync(settings);
        InvalidateCache();
    }

    public async Task RemoveLogoAsync()
    {
        var settings = await LoadAsync();
        settings.LogoBase64 = null;
        settings.LogoMimeType = null;
        await SaveAsync(settings);
    }

    public async Task RemoveBackgroundAsync(int index)
    {
        var settings = await LoadAsync();
        if (settings.Backgrounds is null || index < 1 || index > settings.Backgrounds.Count) return;
        settings.Backgrounds.RemoveAt(index - 1);
        await SaveAsync(settings);
    }

    private void InvalidateCache()
    {
        _settings = null;
        _overrideCss = null;
        _logoBytes = null;
        _bgBytes.Clear();
    }

    // ── CSS generation ────────────────────────────────────────────────────────

    private string GenerateOverrideCss(UICustomizationSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine(_baseOverrideCss);

        bool hasPrimary  = !string.IsNullOrEmpty(settings.PrimaryColor);
        bool hasOnPrimary = !string.IsNullOrEmpty(settings.OnPrimaryColor);
        bool hasHeading   = !string.IsNullOrEmpty(settings.HeadingColor);
        bool hasBodyText  = !string.IsNullOrEmpty(settings.BodyTextColor);
        bool hasLogo      = settings.HasLogo;
        bool hasBg        = settings.BackgroundCount > 0;

        if (!hasPrimary && !hasOnPrimary && !hasHeading && !hasBodyText && !hasLogo && !hasBg)
            return _baseOverrideCss;

        sb.AppendLine();
        sb.AppendLine("/* --- UICustomization overrides --- */");

        if (hasPrimary)
        {
            var primary = settings.PrimaryColor!;
            var darker  = AdjustBrightness(primary, 0.82f);
            var lighter = AdjustBrightness(primary, 1.22f);

            sb.AppendLine($"#main-navbar {{ background-color: {primary} !important; }}");

            // Sidebar active — all known selector patterns + override blue box-shadow
            sb.AppendLine($".sidebar .nav.nav-pills .nav-link.active,");
            sb.AppendLine($".sidebar .nav.nav-pills > li.active > a,");
            sb.AppendLine($".nav.nav-pills.flex-column li.active a,");
            sb.AppendLine($".colorscheme-dark .sidebar .nav.nav-pills .nav-link.active {{");
            sb.AppendLine($"  background-color: {primary} !important;");
            sb.AppendLine($"  box-shadow: 0 2px 6px {primary}4d !important;");
            sb.AppendLine($"}}");
            sb.AppendLine($".sidebar .nav.nav-pills > li:first-child > a {{");
            sb.AppendLine($"  background-color: {primary} !important;");
            sb.AppendLine($"  box-shadow: 0 2px 6px {primary}59 !important;");
            sb.AppendLine($"}}");

            sb.AppendLine($".btn-primary {{ background-color: {primary} !important; border-color: {primary} !important; }}");
            sb.AppendLine($".btn-primary:hover, .btn-primary:focus, .btn-primary:active {{ background-color: {darker} !important; border-color: {darker} !important; }}");

            sb.AppendLine($".ui-list-item:hover, .collapsable-tool .nav.nav-pills.flex-column > li:not(.ui-list-item):hover {{");
            sb.AppendLine($"  box-shadow: inset 3px 0 0 {primary} !important;");
            sb.AppendLine($"}}");

            sb.AppendLine($".form-check-input:checked {{ background-color: {primary} !important; border-color: {primary} !important; }}");
            sb.AppendLine($".form-check-input:focus {{ border-color: {primary} !important; box-shadow: 0 0 0 .25rem {primary}40 !important; }}");

            if (!hasBg)
                sb.AppendLine($".body-layout-login {{ background: linear-gradient(135deg, {darker} 0%, {lighter} 100%) !important; }}");

            sb.AppendLine($".body-layout-login input.form-control:focus {{ border-color: {primary} !important; box-shadow: 0 0 0 3px {primary}2e !important; }}");
        }

        // Sidebar inactive text: HeadingColor if set, else BodyTextColor
        var sidebarTextColor = settings.HeadingColor ?? settings.BodyTextColor;
        if (sidebarTextColor != null)
        {
            sb.AppendLine($".sidebar .nav.nav-pills > li > a,");
            sb.AppendLine($".sidebar .nav.nav-pills .nav-link {{ color: {sidebarTextColor} !important; }}");
            sb.AppendLine($".sidebar .nav.nav-pills > li > a:hover,");
            sb.AppendLine($".sidebar .nav.nav-pills .nav-link:hover {{ background-color: {sidebarTextColor}15 !important; color: {sidebarTextColor} !important; border-color: {sidebarTextColor}30 !important; }}");
        }

        // Text ON primary (navbar, buttons, active sidebar)
        var onPrimary = settings.OnPrimaryColor ?? "#ffffff";
        var onPrimaryEncoded = onPrimary.Replace("#", "%23");
        if (hasOnPrimary)
        {
            sb.AppendLine($"#main-navbar .navbar-brand,");
            sb.AppendLine($"#main-navbar .navbar-nav .nav-link {{ color: {onPrimary} !important; }}");
            sb.AppendLine($"#main-navbar .navbar-nav .nav-link:hover {{ color: {onPrimary} !important; opacity: .85; }}");
            sb.AppendLine($".btn-primary, .btn-primary:hover, .btn-primary:focus, .btn-primary:active {{ color: {onPrimary} !important; }}");
        }
        if (hasPrimary)
        {
            sb.AppendLine($".sidebar .nav.nav-pills .nav-link.active,");
            sb.AppendLine($".sidebar .nav.nav-pills > li.active > a,");
            sb.AppendLine($".sidebar .nav.nav-pills > li:first-child > a {{ color: {onPrimary} !important; }}");

            sb.AppendLine($".form-check-input:checked[type=checkbox] {{ background-image: url(\"data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'%3e%3cpath fill='none' stroke='{onPrimaryEncoded}' stroke-linecap='round' stroke-linejoin='round' stroke-width='3' d='m6 10 3 3 6-6'/%3e%3c/svg%3e\") !important; }}");
            sb.AppendLine($".form-check-input:checked[type=radio] {{ background-image: url(\"data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='-4 -4 8 8'%3e%3ccircle r='2' fill='{onPrimaryEncoded}'/%3e%3c/svg%3e\") !important; }}");
        }

        if (hasHeading)
        {
            var h = settings.HeadingColor!;
            sb.AppendLine($"h1, h2, h3, h4, .h1, .h2, .h3, .h4 {{ color: {h} !important; }}");
            sb.AppendLine($".body-layout-login .card-title {{ color: {h} !important; }}");
            sb.AppendLine($".body-layout-login .form-label,");
            sb.AppendLine($".body-layout-login .form-group label {{ color: {h} !important; }}");
        }

        if (hasBodyText)
        {
            var bt = settings.BodyTextColor!;
            sb.AppendLine($"body, p, td, th, label, .form-label, .form-group label, .text-body {{ color: {bt} !important; }}");
        }

        if (hasBg)
        {
            int count = settings.BackgroundCount;
            for (int slot = 1; slot <= 12; slot++)
            {
                int imgIdx = ((slot - 1) % count) + 1;
                // Endpoint URL — no filesystem dependency
                sb.AppendLine($"body.var-{slot}.body-layout-login {{ background: url('/ui/background/{imgIdx}') center/cover no-repeat !important; }}");
            }
        }

        if (hasLogo)
        {
            sb.AppendLine(".body-layout-login .panel-logo { background-image: url('/ui/logo') !important; }");
            sb.AppendLine(".navbar-brand .icon-banner { content: url('/ui/logo') !important; }");
        }

        return sb.ToString();
    }

    private static string AdjustBrightness(string hex, float factor)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "#" + hex;
        int r = Convert.ToInt32(hex[..2], 16);
        int g = Convert.ToInt32(hex[2..4], 16);
        int b = Convert.ToInt32(hex[4..6], 16);
        r = Math.Clamp((int)(r * factor), 0, 255);
        g = Math.Clamp((int)(g * factor), 0, 255);
        b = Math.Clamp((int)(b * factor), 0, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
