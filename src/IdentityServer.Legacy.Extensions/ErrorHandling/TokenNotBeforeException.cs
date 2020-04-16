using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Token.ErrorHandling
{
    public class TokenNotBeforeException : TokenValidationException 
    {
        public TokenNotBeforeException()
            : base("Not use before")
        {

        }
    }
}
