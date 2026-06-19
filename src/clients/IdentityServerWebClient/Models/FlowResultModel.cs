namespace IdentityServerWebClient.Models;

public class FlowResultModel
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? RawJson { get; set; }
    public string? DecodedAccessTokenPayload { get; set; }
    public string? DecodedIdTokenPayload { get; set; }
    public IList<(string Type, string Value)> Claims { get; set; } = [];
}
