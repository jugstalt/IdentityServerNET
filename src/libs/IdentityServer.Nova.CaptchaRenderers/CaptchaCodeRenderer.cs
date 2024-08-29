using IdentityServer.Nova.Abstractions.Security;
using IdentityServer.Nova.CaptchaRenderers;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System;
using System.IO;

public class CaptchaCodeRenderer : ICaptchaCodeRenderer
{
    private CaptchaCodeRendererOptions _options;

    public CaptchaCodeRenderer(IOptionsMonitor<CaptchaCodeRendererOptions> options)
    {
        _options = options.CurrentValue ?? new CaptchaCodeRendererOptions();
    }

    public byte[] RenderCodeToImage(string captchaCode)
    {
        int width = _options.Width;
        int height = _options.Height;

        using (SKBitmap baseMap = new SKBitmap(width, height))
        using (SKCanvas canvas = new SKCanvas(baseMap))
        {
            canvas.Clear(GetBackgroundColor());

            DrawCaptchaCode(canvas, captchaCode, width, height);
            AdjustRippleEffect(baseMap);
            SmoothImage(baseMap);
            DrawDisorderLine(canvas, width, height);

            using (MemoryStream ms = new MemoryStream())
            using (SKManagedWStream wstream = new SKManagedWStream(ms))
            {
                baseMap.Encode(wstream, SKEncodedImageFormat.Png, 100);
                return ms.ToArray();
            }
        }
    }

    private SKColor GetBackgroundColor()
    {
        switch (_options.BackgroundType)
        {
            case ColorType.Random:
                return GetRandomLightColor();
            case ColorType.Monochrome:
            default:
                return SKColors.White;
        }
    }

    private SKColor GetRandomDeepColor(byte alpha = 255)
    {
        Random rand = new Random();
        int redlow = 160, greenLow = 100, blueLow = 160;

        return new SKColor((byte)rand.Next(redlow), (byte)rand.Next(greenLow), (byte)rand.Next(blueLow), alpha);
    }

    private SKColor GetRandomLightColor()
    {
        Random rand = new Random();
        int low = 180, high = 255;

        int nRend = rand.Next(low, high);
        int nGreen = rand.Next(low, high);
        int nBlue = rand.Next(low, high);

        return new SKColor((byte)nRend, (byte)nGreen, (byte)nBlue);
    }

    private void DrawCaptchaCode(SKCanvas canvas, string captchaCode, int width, int height)
    {
        Random rand = new Random();
        int fontSize = GetFontSize(width, captchaCode.Length);

        using (SKPaint paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.TextSize = fontSize;
            paint.Typeface = SKTypeface.FromFamilyName("Serif", SKFontStyle.Bold);

            for (int i = 0; i < captchaCode.Length; i++)
            {
                paint.Color = _options.TextColorType == ColorType.Random ? GetRandomDeepColor() : SKColors.Black;

                int shiftPx = fontSize / 6;
                float x = i * fontSize + rand.Next(-shiftPx, shiftPx) + fontSize / 4;
                int maxY = height - fontSize;
                float y = rand.Next(0, maxY > 0 ? maxY : 0);

                canvas.DrawText(captchaCode[i].ToString(), x, y + fontSize - fontSize / 8, paint);
            }
        }
    }

    private void DrawDisorderLine(SKCanvas canvas, int width, int height)
    {
        Random rand = new Random();
        using (SKPaint paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.StrokeWidth = _options.DisorderLinePenWidth;

            for (int i = 0; i < rand.Next(7, 12); i++)
            {
                paint.Color = GetRandomDeepColor(60);

                SKPoint startPoint = new SKPoint(rand.Next(0, width), rand.Next(0, height));
                SKPoint endPoint = new SKPoint(rand.Next(0, width), rand.Next(0, height));
                canvas.DrawLine(startPoint, endPoint, paint);

                SKPoint bezierPoint1 = new SKPoint(rand.Next(0, width), rand.Next(0, height));
                SKPoint bezierPoint2 = new SKPoint(rand.Next(0, width), rand.Next(0, height));

                using (var path = new SKPath())
                {
                    path.MoveTo(startPoint);
                    path.QuadTo(bezierPoint1, endPoint);
                    canvas.DrawPath(path, paint);
                }
            }
        }
    }

    private void AdjustRippleEffect(SKBitmap baseMap)
    {
        int width = baseMap.Width;
        int height = baseMap.Height;
        SKBitmap bSrc = baseMap.Copy();
        SKPoint[,] pt = new SKPoint[width, height];

        double nWave = 6;
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                double xo = nWave * Math.Sin(2.0 * Math.PI * y / 128.0);
                double yo = nWave * Math.Cos(2.0 * Math.PI * x / 128.0);

                double newX = x + xo;
                double newY = y + yo;

                if (newX >= 0 && newX < width)
                {
                    pt[x, y].X = (float)newX;
                }
                else
                {
                    pt[x, y].X = 0;
                }

                if (newY >= 0 && newY < height)
                {
                    pt[x, y].Y = (float)newY;
                }
                else
                {
                    pt[x, y].Y = 0;
                }
            }
        }

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                SKColor color = bSrc.GetPixel((int)pt[x, y].X, (int)pt[x, y].Y);
                baseMap.SetPixel(x, y, color);
            }
        }
    }

    private void SmoothImage(SKBitmap baseMap)
    {
        using (var surface = SKSurface.Create(baseMap.Info))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.SetMatrix(SKMatrix.CreateScale(1.01f, 1.01f)); // Leichte Vergrößerung um 1%
            canvas.DrawBitmap(baseMap, 0, 0);
            canvas.Flush();
        }
    }

    private int GetFontSize(int imageWidth, int captchCodeCount)
    {
        var averageSize = imageWidth / captchCodeCount;
        return Convert.ToInt32(averageSize);
    }
}
