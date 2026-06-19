namespace IdentityServerWebClient.Models;

public class ClientCredentialsModel
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "";
    public FlowResultModel? Result { get; set; }
}
