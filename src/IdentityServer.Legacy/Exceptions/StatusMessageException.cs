using System;

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
