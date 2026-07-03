using System.ComponentModel.DataAnnotations;

namespace IdentityServer;

public class LoginIdentifierInputModel
{
    [Required]
    [Display(Name = "Username or Email")]
    public string Username { get; set; }
    public string ReturnUrl { get; set; }
}
