using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.Realms.EditRealm;

public static class EditRealmNavPages
{
    public static string Index => "Index";
    public static string Domains => "Domains";
    public static string RealmUsers => "RealmUsers";
    public static string DeleteRealm => "DeleteRealm";

    public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);
    public static string DomainsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Domains);
    public static string RealmUsersNavClass(ViewContext viewContext) => PageNavClass(viewContext, RealmUsers);
    public static string DeleteRealmNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeleteRealm);

    private static string PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
