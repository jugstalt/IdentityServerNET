using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Token.ErrorHandling
{
    public class InvalidSignatureException : TokenValidationException
    {
        public InvalidSignatureException()
            : base("Invalid jwt signature")
        {

        }
    }
}
