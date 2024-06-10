namespace IdentityServer.Nova.Token.ErrorHandling
{
    public class InvalidSignatureException : TokenValidationException
    {
        public InvalidSignatureException()
            : base("Invalid jwt signature")
        {

        }
    }
}
