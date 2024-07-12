﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public static class EditUserNavPages
{
    public static string Index => "Index";

    public static string UserEmail => "UserEmail";

    public static string UserRoles => "UserRoles";

    public static string SetPassword => "SetPassword";

    public static string DeleteUser => "DeleteUser";

    public static string UserEmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, UserEmail);

    public static string UserRolesNavClass(ViewContext viewContext) => PageNavClass(viewContext, UserRoles);

    public static string SetPasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, SetPassword);

    public static string DeleteUserNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeleteUser);

    private static string PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
