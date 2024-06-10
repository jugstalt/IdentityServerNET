using System;

namespace IdentityServer.Nova.Token.ErrorHandling
{
    public class TokenValidationException : Exception
    {
        public TokenValidationException(string message) :
            base($"Token validation: {message}")
        {

        }
    }
}
