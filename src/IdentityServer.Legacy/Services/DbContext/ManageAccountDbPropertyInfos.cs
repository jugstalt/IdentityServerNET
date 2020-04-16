using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class ManageAccountDbPropertyInfos : DbPropertyInfos
    {
        public bool AllowDelete { get; set; }
        public bool ShowChangeEmailPage { get; set; }
        public bool ShowChangePasswordPage { get; set; }
        public bool ShowTfaPage { get; set; }
    }
}
