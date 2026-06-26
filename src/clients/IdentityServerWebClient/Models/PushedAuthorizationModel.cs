namespace IdentityServerWebClient.Models;

public class PushedAuthorizationInputModel
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "openid profile";
    public string RedirectUri { get; set; } = "";
}

public class PushedAuthorizationPushedModel : PushedAuthorizationInputModel
{
    public string RequestUri { get; set; } = "";
    public int ExpiresIn { get; set; }
    public string AuthorizeUrl { get; set; } = "";
    public string State { get; set; } = "";
    public string CodeVerifier { get; set; } = "";
    public string CodeChallenge { get; set; } = "";
    public string ParRequestRaw { get; set; } = "";
    public string ParResponseRaw { get; set; } = "";
}

public class PushedAuthorizationCallbackModel
{
    public string Code { get; set; } = "";
    public string State { get; set; } = "";
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string CodeVerifier { get; set; } = "";
    public FlowResultModel? Result { get; set; }
}
