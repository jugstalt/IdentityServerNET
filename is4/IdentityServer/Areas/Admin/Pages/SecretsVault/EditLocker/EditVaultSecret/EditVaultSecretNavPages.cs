using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker.EditVaultSecret;

public static class EditVaultSecretNavPages
{
    public static string Index => "Index";

    public static string Versions => "Versions";

    public static string Delete => "Delete";

    public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

    public static string VersionsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Versions);

    public static string DeleteNavClass(ViewContext viewContext) => PageNavClass(viewContext, Delete);

    private static string PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
