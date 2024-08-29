namespace IdentityServer.Nova.Abstractions.Security;

public interface ICaptchaCodeRenderer
{
    byte[] RenderCodeToImage(string captchaCode);
}
