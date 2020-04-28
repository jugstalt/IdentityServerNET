using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Models
{
    public class VaultSecretVersion
    {
        public long VersionTimeStamp { get; set; }

        public string Secret { get; set; }
    }
}
