namespace IdentityServerWebClient.Models;

public class RopcModel
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "openid profile";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public FlowResultModel? Result { get; set; }
}
