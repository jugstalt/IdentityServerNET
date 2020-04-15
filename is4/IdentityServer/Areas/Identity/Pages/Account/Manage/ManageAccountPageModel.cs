using IdentityServer.Legacy;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public class ManageAccountPageModel : PageModel
    {
        protected IUserDbContext _userDbContext;

        protected ManageAccountPageModel(
            string category,
            IUserDbContext userManager,
            IOptions<UserDbContextConfiguration> userDbContextConfiguration)
        {
            Category = category;
            _userDbContext = userManager;
            OptionalPropertyInfos =
                userDbContextConfiguration?.Value?.ManageAccountProperties;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public ApplicationUser ApplicationUser { get; set; } 

        public string Category { get; set; }

        public DbPropertyInfos OptionalPropertyInfos { get; set; }

        async protected Task LoadUserAsync()
        {
            this.ApplicationUser = await _userDbContext.FindByNameAsync(User.Identity?.Name, new System.Threading.CancellationToken());
        }

        public object GetPropertyValue(DbPropertyInfo dbPropertyInfo)
        {
            if (this.ApplicationUser == null)
                return null;

            var propertyInfo = typeof(ApplicationUser).GetProperty(dbPropertyInfo.Name);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(this.ApplicationUser);
            }

            if (!String.IsNullOrWhiteSpace(dbPropertyInfo.ClaimName))
            {
                var claim = this.ApplicationUser.Claims
                                .Where(c => c.Type == dbPropertyInfo.ClaimName)
                                .FirstOrDefault();

                if (claim != null)
                {
                    // ToDo: Change Type (claim.ValueType)
                    return claim.Value;
                }
            }

            return null;
        }

        public string GetPropertyType(DbPropertyInfo dbPropertyInfo)
        {
            if (dbPropertyInfo.PropertyType == typeof(DateTime))
                return "date";
            if (dbPropertyInfo.PropertyType == typeof(bool))
                return "checkbox";

            return "text";
        }
    }
}
