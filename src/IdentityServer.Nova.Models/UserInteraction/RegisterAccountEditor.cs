namespace IdentityServer.Nova.Models.UserInteraction;

public class RegisterAccountEditor
{
    public bool ShowGivenName { get; set; }
    public bool ShowFamilyName { get; set; }
    public bool ShowCompany { get; set; }

    public string PromotionCode { get; set; } = "";
}
