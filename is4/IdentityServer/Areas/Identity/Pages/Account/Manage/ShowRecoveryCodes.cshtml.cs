﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy;
using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public class ShowRecoveryCodesModel : ManageAccountPageModel
    {
        public ShowRecoveryCodesModel(IUserStoreFactory userStoreFactory)
            : base(userStoreFactory)
        {

        }

        [TempData]
        public string[] RecoveryCodes { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult OnGet()
        {
            if (RecoveryCodes == null || RecoveryCodes.Length == 0)
            {
                return RedirectToPage("./TwoFactorAuthentication");
            }

            return Page();
        }
    }
}
