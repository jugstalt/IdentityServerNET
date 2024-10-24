using Microsoft.AspNetCore.Identity;
using System;

namespace IdentityServerNET.Models;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = "";
    public DateTime CreateDate { get; set; }
}
