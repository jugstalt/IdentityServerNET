﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole;

public static class EditRoleNavPages
{
    public static string Index => "Index";

    public static string RoleUsers => "RoleUsers";

    public static string DeleteRole => "DeleteRole";

    public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

    public static string RoleUsersNavClass(ViewContext viewContext) => PageNavClass(viewContext, RoleUsers);

    public static string DeleteRoleNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeleteRole);

    private static string PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
