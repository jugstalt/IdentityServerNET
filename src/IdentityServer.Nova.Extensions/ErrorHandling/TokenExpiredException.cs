namespace IdentityServer.Nova.Token.ErrorHandling
{
    public class TokenExpiredException : TokenValidationException
    {
        public TokenExpiredException()
            : base("token expired")
        {

        }
    }
}
