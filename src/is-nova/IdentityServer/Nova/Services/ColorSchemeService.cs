using Microsoft.AspNetCore.Http;
using System;

namespace IdentityServer.Nova.Services;

public class ColorSchemeService
{
    private const string CookieName = "identityserver.nova.colorscheme";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ColorSchemeService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        var request = _httpContextAccessor.HttpContext?.Request;
        var colorSchemeString = request?.Cookies[CookieName];

        this.CurrentColorScheme =
            Enum.TryParse<ColorSchemes>(colorSchemeString, out var colorScheme)
            ? colorScheme
            : ColorSchemes.Dark;
    }

    public ColorSchemes CurrentColorScheme { get; private set; }

    public void SetColorScheme(ColorSchemes colorScheme)
    {
        this.CurrentColorScheme = colorScheme;

        var response = _httpContextAccessor.HttpContext?.Response;

        if (response != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // only https
                SameSite = SameSiteMode.Lax, 
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            };

            response.Cookies.Append(CookieName, colorScheme.ToString(), cookieOptions);
        }
    }
    
    #region Icons

    public string TileIcon(string icon, int size = 48)
        => $"ui-tile-icon sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";


    public string LeftIcon(string icon, int size = 32)
        => $"ui-icon left sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";

    public string NavIcon(string icon, int size = 16)
        => $"nav-icon sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";
    

    public string Icon(string icon, int size=32)
        => $"sketchpen-icon-basic-bg-{Scheme}-{size} {icon}";

    #endregion

    #region Helper

    private string Scheme =>
       CurrentColorScheme switch
       {
           ColorSchemes.Dark => "dark",
           _ => "light"
       };

    #endregion
}
