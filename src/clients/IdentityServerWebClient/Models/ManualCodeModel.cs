namespace IdentityServerWebClient.Models;

public class ManualCodeInputModel
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "openid profile";
    public string RedirectUri { get; set; } = "";
}

public class ManualCodeBuildModel : ManualCodeInputModel
{
    public string AuthorizeUrl { get; set; } = "";
    public string State { get; set; } = "";
    public string CodeVerifier { get; set; } = "";
    public string CodeChallenge { get; set; } = "";
}

public class ManualCodeCallbackModel
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
