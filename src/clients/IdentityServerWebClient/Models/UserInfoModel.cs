namespace IdentityServerWebClient.Models;

public class UserInfoModel
{
    public string Authority { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public FlowResultModel? Result { get; set; }
}
