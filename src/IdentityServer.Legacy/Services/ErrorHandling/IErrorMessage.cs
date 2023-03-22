namespace IdentityServer.Legacy.Services.ErrorHandling
{
    public interface IErrorMessage
    {
        string LastErrorMessage { get; }

        bool HasErrors { get; }

        void ClearErrors();
    }
}
