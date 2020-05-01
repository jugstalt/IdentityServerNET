using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.Security
{
    public interface ICaptchCodeRenderer
    {
        byte[] RenderCodeToImage(string captchaCode, int width = 200, int height = 60);
    }
}
