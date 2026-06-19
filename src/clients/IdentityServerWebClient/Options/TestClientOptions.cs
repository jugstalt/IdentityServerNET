namespace IdentityServerWebClient.Options;

public class TestClientOptions
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scopes { get; set; } = "openid profile";
    public string ApiClientId { get; set; } = "";
    public string ApiClientSecret { get; set; } = "";
    public string ApiScopes { get; set; } = "";
    public string IntrospectionClientId { get; set; } = "";
    public string IntrospectionClientSecret { get; set; } = "";
}
