using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker;

public static class EditLockerNavPages
{
    public static string Index => "Index";

    public static string Secrets => "Secrets";

    public static string Delete => "Delete";

    public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

    public static string SecretsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Secrets);

    public static string DeleteNavClass(ViewContext viewContext) => PageNavClass(viewContext, Delete);

    private static string PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
