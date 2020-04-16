using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Token.ErrorHandling
{
    public class TokenExpiredException : TokenValidationException
    {
        public TokenExpiredException() 
            : base("token expired")
        {

        }
    }
}
