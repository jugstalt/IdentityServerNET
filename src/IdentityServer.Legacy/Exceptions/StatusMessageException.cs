using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Exceptions
{
    public class StatusMessageException : Exception
    {
        public StatusMessageException(string message)
            : base(message)
        {

        }
    }
}
