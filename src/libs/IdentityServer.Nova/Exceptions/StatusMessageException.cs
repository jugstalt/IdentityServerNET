using System;

namespace IdentityServer.Nova.Exceptions;

public class StatusMessageException : Exception
{
    public StatusMessageException(string message)
        : base(message)
    {

    }
}
