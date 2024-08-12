namespace IdentityServer.Nova.Abstractions.Security;

public interface ICaptchCodeRenderer
{
    byte[] RenderCodeToImage(string captchaCode);
}
