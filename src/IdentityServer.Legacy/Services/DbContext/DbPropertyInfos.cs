using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class DbPropertyInfos
    {
        public bool CanDelete { get; set; }
        public IEnumerable<DbPropertyInfo> PropertyInfos { get; set; }
    }
}
