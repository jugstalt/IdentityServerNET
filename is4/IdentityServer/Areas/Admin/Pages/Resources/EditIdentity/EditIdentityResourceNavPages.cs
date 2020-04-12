using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class EditIdentityResourceNavPages
    {
        public static string Index => "Index";

        public static string UserClaims => "User Claims";

        public static string Options => "Options";

        public static string DeleteIdentity => "DeleteIdentity";

        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        public static string UserClaimsNavClass(ViewContext viewContext) => PageNavClass(viewContext, UserClaims);

        public static string OptionsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Options);

        public static string DeleteIdentityNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeleteIdentity);

        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
    }
}
