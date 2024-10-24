using System;

namespace IdentityServerNET.Exceptions;

public class StatusMessageException : Exception
{
    public StatusMessageException(string message)
        : base(message)
    {

    }
}
