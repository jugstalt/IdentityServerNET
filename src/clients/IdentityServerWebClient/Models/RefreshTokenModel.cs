namespace IdentityServerWebClient.Models;

public class RefreshTokenModel
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public FlowResultModel? Result { get; set; }
}
