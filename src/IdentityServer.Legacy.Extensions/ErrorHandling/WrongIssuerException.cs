using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Token.ErrorHandling
{
    public class WrongIssuerException : TokenValidationException
    {
        public WrongIssuerException()
            : base("wrong issuer")
        {

        }
    }
}
