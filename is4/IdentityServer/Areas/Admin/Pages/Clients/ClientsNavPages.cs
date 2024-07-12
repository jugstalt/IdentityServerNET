using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.Clients;

public static class ClientsNavPages
{
    public static string Index => "Index";

    public static string Clients => "Clients";

    public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

    public static string ClientsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Clients);


    private static string PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
