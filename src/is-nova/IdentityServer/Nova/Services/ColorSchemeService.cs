using Microsoft.AspNetCore.Http;
using System;

namespace IdentityServer.Nova.Services;

internal class ColorSchemeService
{
    private const string CookieName = "identityserver.nova.colorscheme";

    public ColorSchemeService(IHttpContextAccessor httpContextAccessor)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        var colorSchemeString = request.Cookies[CookieName];

        this.CurrentColorScheme =
            Enum.TryParse<ColorSchemes>(colorSchemeString, out var colorScheme)
            ? colorScheme
            : ColorSchemes.Dark;
    }

    public ColorSchemes CurrentColorScheme { get; private set; }

    private string Scheme =>
        CurrentColorScheme switch
        {
            ColorSchemes.Dark => "dark",
            _ => "light"
        };

    public string TileIcon(string icon, int size = 48)
        => $"ui-tile-icon sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";


    public string LeftIcon(string icon, int size = 32)
        => $"ui-icon left sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";

    public string NavIcon(string icon, int size = 16)
        => $"nav-icon sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";
    

    public string Icon(string icon, int size=32)
        => $"sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";
    
}
