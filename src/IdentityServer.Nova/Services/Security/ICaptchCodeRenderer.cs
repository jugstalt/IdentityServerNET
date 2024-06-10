namespace IdentityServer.Nova.Services.Security
{
    public interface ICaptchCodeRenderer
    {
        byte[] RenderCodeToImage(string captchaCode);
    }
}
