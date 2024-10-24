using IdentityServerNET.Models.UserInteraction;

namespace IdentityServerNET.Abstractions.Services;

public class UserDbContextConfiguration
{
    public string ConnectionString { get; set; } = "";

    public ManageAccountEditor? ManageAccountEditor { get; set; }
    public AdminAccountEditor? AdminAccountEditor { get; set; }
    public RegisterAccountEditor? RegisterAccountEditor { get; set; }
}
