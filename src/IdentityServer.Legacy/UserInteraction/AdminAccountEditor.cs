using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.UserInteraction
{
    public class AdminAccountEditor: EditorInfoCollection
    {
        public bool AllowDelete { get; set; }

        public bool AllowSetPassword { get; set; }
    }
}
