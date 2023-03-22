using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class EditApiResourceNavPages
    {
        public static string Index => "Index";

        public static string Scopes => "Scopes";

        public static string Secrets => "Secrets";

        public static string Grants => "Grants";

        public static string Properties => "Properties";

        public static string Collections => "Collections";

        public static string Options => "Options";

        public static string DeleteApi => "DeleteApi";

        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        public static string ScopesNavClass(ViewContext viewContext) => PageNavClass(viewContext, Scopes);

        public static string SecretsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Secrets);

        public static string GrantsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Grants);

        public static string CollectionsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Collections);

        public static string PropertiesNavClass(ViewContext viewContext) => PageNavClass(viewContext, Properties);

        public static string OptionsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Options);

        public static string DeleteApiNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeleteApi);

        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
    }
}
