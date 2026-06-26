using IdentityServerNET.Abstractions.Security;
using SkiaSharp;
using System;
using System.IO;

namespace IdentityServerNET.CaptchaRenderers;

/// <summary>
/// Modern CAPTCHA renderer with clean design: subtle gradient background,
/// individual character rotation/offset, and light interference lines.
/// </summary>
public class ModernCaptchaCodeRenderer : ICaptchaCodeRenderer
{
    private static readonly SKColor[] _palette = new SKColor[]
    {
        new SKColor(0x1a, 0x6f, 0xc4),  // blue
        new SKColor(0x2e, 0x7d, 0x32),  // green
        new SKColor(0x6a, 0x1b, 0x9a),  // purple
        new SKColor(0xc6, 0x28, 0x28),  // red
        new SKColor(0xe6, 0x5c, 0x00),  // orange
        new SKColor(0x00, 0x69, 0x5c),  // teal
    };

    private readonly Random _rng = new();

    public byte[] RenderCodeToImage(string captchaCode)
    {
        const int width = 200;
        const int height = 60;
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        DrawBackground(canvas, width, height);
        DrawNoiseLines(canvas, width, height);
        DrawCharacters(canvas, captchaCode, width, height);
        DrawNoiseDots(canvas, width, height);

        using var ms = new MemoryStream();
        using var wstream = new SKManagedWStream(ms);
        bitmap.Encode(wstream, SKEncodedImageFormat.Png, 100);
        return ms.ToArray();
    }

    private void DrawBackground(SKCanvas canvas, int w, int h)
    {
        using var paint = new SKPaint { IsAntialias = true };

        // very light grey-blue gradient
        paint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(w, h),
            new SKColor[] { new SKColor(0xf0, 0xf4, 0xf8), new SKColor(0xe2, 0xea, 0xf4) },
            (float[])null,
            SKShaderTileMode.Clamp);

        canvas.DrawRect(SKRect.Create(w, h), paint);
    }

    private void DrawNoiseLines(SKCanvas canvas, int w, int h)
    {
        using var paint = new SKPaint { IsAntialias = true, StrokeWidth = 1.2f };

        int lineCount = _rng.Next(4, 7);
        for (int i = 0; i < lineCount; i++)
        {
            var color = _palette[_rng.Next(_palette.Length)];
            paint.Color = new SKColor(color.Red, color.Green, color.Blue, 40);

            float x1 = _rng.Next(0, w / 2);
            float y1 = _rng.Next(0, h);
            float x2 = _rng.Next(w / 2, w);
            float y2 = _rng.Next(0, h);
            float cx = _rng.Next(0, w);
            float cy = _rng.Next(0, h);

            using var builder = new SKPathBuilder();
            builder.MoveTo(x1, y1);
            builder.QuadTo(cx, cy, x2, y2);
            using var path = builder.Snapshot();
            canvas.DrawPath(path, paint);
        }
    }

    private void DrawNoiseDots(SKCanvas canvas, int w, int h)
    {
        using var paint = new SKPaint { IsAntialias = true };

        int dotCount = _rng.Next(60, 100);
        for (int i = 0; i < dotCount; i++)
        {
            var color = _palette[_rng.Next(_palette.Length)];
            paint.Color = new SKColor(color.Red, color.Green, color.Blue, (byte)_rng.Next(30, 80));
            float r = _rng.Next(1, 3);
            canvas.DrawCircle(_rng.Next(0, w), _rng.Next(0, h), r, paint);
        }
    }

    private void DrawCharacters(SKCanvas canvas, string code, int w, int h)
    {
        if (string.IsNullOrEmpty(code)) return;

        float slotW = (float)w / code.Length;
        float fontSize = Math.Min(slotW * 0.72f, h * 0.62f);

        using var font = new SKFont
        {
            Size = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                       ?? SKTypeface.Default
        };

        using var paint = new SKPaint { IsAntialias = true };

        for (int i = 0; i < code.Length; i++)
        {
            var color = _palette[_rng.Next(_palette.Length)];
            paint.Color = new SKColor(color.Red, color.Green, color.Blue, 220);

            // center of this character's slot
            float cx = slotW * i + slotW / 2f;
            float cy = h / 2f + _rng.Next(-6, 7);

            float rotation = _rng.Next(-18, 19);
            float scale = 0.88f + (float)_rng.NextDouble() * 0.24f;

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.RotateDegrees(rotation);
            canvas.Scale(scale);

            string ch = code[i].ToString();
            float charW = font.MeasureText(ch, paint);
            canvas.DrawText(ch, -charW / 2f, fontSize / 3f, SKTextAlign.Left, font, paint);

            canvas.Restore();
        }
    }
}
