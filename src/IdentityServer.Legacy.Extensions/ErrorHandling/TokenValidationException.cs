using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Token.ErrorHandling
{
    public class TokenValidationException : Exception
    {
        public TokenValidationException(string message) :
            base($"Token validation: { message }")
        {

        }
    }
}
