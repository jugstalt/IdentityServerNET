using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IdentityServerNET.Abstractions.UI;

public class UICustomizationSettings
{
    public string? ApplicationTitle { get; set; }
    public string? PrimaryColor { get; set; }
    public string? OnPrimaryColor { get; set; }
    public string? HeadingColor { get; set; }
    public string? BodyTextColor { get; set; }

    // Stored inline as base64 — no filesystem needed
    public string? LogoBase64 { get; set; }
    public string? LogoMimeType { get; set; }
    public List<UIBackgroundImage>? Backgrounds { get; set; }

    [JsonIgnore]
    public bool HasLogo => !string.IsNullOrEmpty(LogoBase64);
    [JsonIgnore]
    public int BackgroundCount => Backgrounds?.Count ?? 0;
}

public class UIBackgroundImage
{
    public string Base64 { get; set; } = "";
    public string MimeType { get; set; } = "image/jpeg";
}
