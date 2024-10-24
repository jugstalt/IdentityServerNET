namespace IdentityServerNET.Abstractions.Security;

public interface ICaptchaCodeRenderer
{
    byte[] RenderCodeToImage(string captchaCode);
}
