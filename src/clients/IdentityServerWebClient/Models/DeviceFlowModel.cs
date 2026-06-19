namespace IdentityServerWebClient.Models;

public class DeviceFlowInputModel
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "openid profile";
}

public class DeviceFlowViewModel : DeviceFlowInputModel
{
    public string DeviceCode { get; set; } = "";
    public string UserCode { get; set; } = "";
    public string VerificationUri { get; set; } = "";
    public string? VerificationUriComplete { get; set; }
    public int Interval { get; set; } = 5;
    public int ExpiresIn { get; set; }
}
