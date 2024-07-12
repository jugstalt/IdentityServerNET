namespace IdentityServer.Nova.Services;

public class AuthorizationContext
{
    public string ReturnUrl { get; set; }
    public string ClientId { get; set; }
    public string ClientName { get; set; }
}
