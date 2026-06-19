namespace IdentityServerWebClient.Models;

public class TokenInspectorModel
{
    public string Token { get; set; } = "";
    public string Authority { get; set; } = "";
    public string IntrospectionClientId { get; set; } = "";
    public string IntrospectionClientSecret { get; set; } = "";
    public string? Header { get; set; }
    public string? Payload { get; set; }
    public string? IntrospectionResult { get; set; }
    public bool? IsActive { get; set; }
    public string? Error { get; set; }
}
