using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IdentityServerNET.Models;

/// <summary>
/// Realm-specific UI appearance settings stored inside <see cref="RealmModel"/>.
/// Mirrors <c>UICustomizationSettings</c> but lives in the Models layer to avoid a circular
/// project dependency (Abstractions → Models).
/// </summary>
public class RealmAppearanceSettings
{
    public string? ApplicationTitle { get; set; }
    public string? PrimaryColor { get; set; }
    public string? OnPrimaryColor { get; set; }
    public string? HeadingColor { get; set; }
    public string? BodyTextColor { get; set; }

    public string? LogoBase64 { get; set; }
    public string? LogoMimeType { get; set; }
    public List<RealmAppearanceBackground>? Backgrounds { get; set; }

    [JsonIgnore]
    public bool HasLogo => !string.IsNullOrEmpty(LogoBase64);
    [JsonIgnore]
    public int BackgroundCount => Backgrounds?.Count ?? 0;
}

public class RealmAppearanceBackground
{
    public string Base64 { get; set; } = "";
    public string MimeType { get; set; } = "image/jpeg";
}
