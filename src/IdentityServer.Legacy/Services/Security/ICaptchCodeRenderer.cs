namespace IdentityServer.Legacy.Services.Security
{
    public interface ICaptchCodeRenderer
    {
        byte[] RenderCodeToImage(string captchaCode);
    }
}
