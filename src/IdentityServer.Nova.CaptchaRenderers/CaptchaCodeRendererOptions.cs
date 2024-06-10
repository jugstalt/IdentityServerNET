namespace IdentityServer.Nova.CaptchaRenderers
{
    public class CaptchaCodeRendererOptions
    {
        public CaptchaCodeRendererOptions()
        {
            Width = 200;
            Height = 60;

            BackgroundType =
            TextColorType = ColorType.Random;

            DisorderLinePenWidth = 4;
        }

        public int Width { get; set; }
        public int Height { get; set; }

        public ColorType BackgroundType { get; set; }
        public ColorType TextColorType { get; set; }

        public int DisorderLinePenWidth { get; set; }
    }
}
