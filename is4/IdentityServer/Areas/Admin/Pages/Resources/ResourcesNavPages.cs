using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.Resources;

public static class ResourcesNavPages
{
    public static string Index => "Index";

    public static string Apis => "Apis";

    public static string Identities => "Identities";

    public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

    public static string ApisNavClass(ViewContext viewContext) => PageNavClass(viewContext, Apis);

    public static string IdentitiesNavClass(ViewContext viewContext) => PageNavClass(viewContext, Identities);


    private static string PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
