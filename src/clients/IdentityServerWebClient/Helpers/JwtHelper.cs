using System.Text;
using System.Text.Json;

namespace IdentityServerWebClient.Helpers;

public static class JwtHelper
{
    public static (string? Header, string? Payload) Decode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return (null, null);
        var parts = token.Split('.');
        if (parts.Length < 2) return (null, null);
        return (DecodeBase64(parts[0]), DecodeBase64(parts[1]));
    }

    private static string? DecodeBase64(string part)
    {
        try
        {
            var padded = part.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(padded);
            var json = Encoding.UTF8.GetString(bytes);
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return null;
        }
    }
}
