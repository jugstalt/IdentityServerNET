using System;

namespace IdentityServerNET.Token.ErrorHandling;

public class TokenValidationException : Exception
{
    public TokenValidationException(string message) :
        base($"Token validation: {message}")
    {

    }
}
