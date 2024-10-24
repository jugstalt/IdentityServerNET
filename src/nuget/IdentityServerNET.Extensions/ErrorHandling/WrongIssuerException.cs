namespace IdentityServerNET.Token.ErrorHandling;

public class WrongIssuerException : TokenValidationException
{
    public WrongIssuerException()
        : base("wrong issuer")
    {

    }
}
