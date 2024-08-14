﻿//using IdentityServer.Nova.Abstractions.Security;
//using Microsoft.Extensions.Options;
//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;

//namespace IdentityServer.Nova.CaptchaRenderers;

//public class CaptchaCodeRenderer : ICaptchCodeRenderer
//{
//    private CaptchaCodeRendererOptions _options;
//    public CaptchaCodeRenderer(IOptionsMonitor<CaptchaCodeRendererOptions> options)
//    {
//        _options = options.CurrentValue ?? new CaptchaCodeRendererOptions();
//    }

//    public byte[] RenderCodeToImage(string captchaCode)
//    {
//        int width = _options.Width;
//        int height = _options.Height;

//        using (Bitmap baseMap = new Bitmap(width, height))
//        using (Graphics graph = Graphics.FromImage(baseMap))
//        {
//            graph.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
//            graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

//            Random rand = new Random();

//            var bgColor = Color.White;

//            switch (_options.BackgroundType)
//            {
//                case ColorType.Monochrome:
//                    bgColor = Color.White;
//                    break;
//                case ColorType.Random:
//                    bgColor = GetRandomLightColor();
//                    break;
//            }
//            graph.Clear(bgColor);

//            DrawCaptchaCode();

//            AdjustRippleEffect();
//            DrawDisorderLine(bgColor);

//            MemoryStream ms = new MemoryStream();

//            baseMap.Save(ms, ImageFormat.Png);

//            return ms.ToArray();

//            int GetFontSize(int imageWidth, int captchCodeCount)
//            {
//                var averageSize = imageWidth / captchCodeCount;

//                return Convert.ToInt32(averageSize);
//            }

//            Color GetRandomDeepColor()
//            {
//                int redlow = 160, greenLow = 100, blueLow = 160;

//                return Color.FromArgb(rand.Next(redlow), rand.Next(greenLow), rand.Next(blueLow));
//            }

//            Color GetRandomLightColor()
//            {
//                int low = 180, high = 255;

//                int nRend = rand.Next(high) % (high - low) + low;
//                int nGreen = rand.Next(high) % (high - low) + low;
//                int nBlue = rand.Next(high) % (high - low) + low;

//                return Color.FromArgb(nRend, nGreen, nBlue);
//            }

//            void DrawCaptchaCode()
//            {
//                SolidBrush fontBrush = new SolidBrush(Color.Black);
//                int fontSize = GetFontSize(width, captchaCode.Length);
//                Font font = new Font(FontFamily.GenericSerif, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
//                for (int i = 0; i < captchaCode.Length; i++)
//                {
//                    fontBrush.Color =
//                        _options.TextColorType == ColorType.Random ?
//                                GetRandomDeepColor() :
//                                Color.Black;

//                    int shiftPx = fontSize / 6;

//                    float x = i * fontSize + rand.Next(-shiftPx, shiftPx) + rand.Next(-shiftPx, shiftPx);
//                    int maxY = height - fontSize;
//                    if (maxY < 0)
//                    {
//                        maxY = 0;
//                    }

//                    float y = rand.Next(0, maxY);

//                    graph.DrawString(captchaCode[i].ToString(), font, fontBrush, x, y);
//                }
//            }

//            void DrawDisorderLine(Color color)
//            {
//                Pen linePen = new Pen(new SolidBrush(Color.Black), _options.DisorderLinePenWidth);
//                for (int i = 0; i < rand.Next(3, 5); i++)
//                {
//                    linePen.Color = color; //GetRandomDeepColor();

//                    Point startPoint = new Point(rand.Next(0, width), rand.Next(0, height));
//                    Point endPoint = new Point(rand.Next(0, width), rand.Next(0, height));
//                    graph.DrawLine(linePen, startPoint, endPoint);

//                    Point bezierPoint1 = new Point(rand.Next(0, width), rand.Next(0, height));
//                    Point bezierPoint2 = new Point(rand.Next(0, width), rand.Next(0, height));

//                    graph.DrawBezier(linePen, startPoint, bezierPoint1, bezierPoint2, endPoint);
//                }
//            }

//            void AdjustRippleEffect()
//            {
//                short nWave = 6;
//                int nWidth = baseMap.Width;
//                int nHeight = baseMap.Height;

//                Point[,] pt = new Point[nWidth, nHeight];

//                double newX, newY;
//                double xo, yo;

//                for (int x = 0; x < nWidth; ++x)
//                {
//                    for (int y = 0; y < nHeight; ++y)
//                    {
//                        xo = (nWave * Math.Sin(2.0 * 3.1415 * y / 128.0));
//                        yo = (nWave * Math.Cos(2.0 * 3.1415 * x / 128.0));

//                        newX = (x + xo);
//                        newY = (y + yo);

//                        if (newX > 0 && newX < nWidth)
//                        {
//                            pt[x, y].X = (int)newX;
//                        }
//                        else
//                        {
//                            pt[x, y].X = 0;
//                        }


//                        if (newY > 0 && newY < nHeight)
//                        {
//                            pt[x, y].Y = (int)newY;
//                        }
//                        else
//                        {
//                            pt[x, y].Y = 0;
//                        }
//                    }
//                }

//                Bitmap bSrc = (Bitmap)baseMap.Clone();

//                BitmapData bitmapData = baseMap.LockBits(new Rectangle(0, 0, baseMap.Width, baseMap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
//                BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

//                int scanline = bitmapData.Stride;

//                IntPtr Scan0 = bitmapData.Scan0;
//                IntPtr SrcScan0 = bmSrc.Scan0;

//                unsafe
//                {
//                    byte* p = (byte*)(void*)Scan0;
//                    byte* pSrc = (byte*)(void*)SrcScan0;

//                    int nOffset = bitmapData.Stride - baseMap.Width * 3;

//                    int xOffset, yOffset;

//                    for (int y = 0; y < nHeight; ++y)
//                    {
//                        for (int x = 0; x < nWidth; ++x)
//                        {
//                            xOffset = pt[x, y].X;
//                            yOffset = pt[x, y].Y;

//                            if (yOffset >= 0 && yOffset < nHeight && xOffset >= 0 && xOffset < nWidth)
//                            {
//                                p[0] = pSrc[(yOffset * scanline) + (xOffset * 3)];
//                                p[1] = pSrc[(yOffset * scanline) + (xOffset * 3) + 1];
//                                p[2] = pSrc[(yOffset * scanline) + (xOffset * 3) + 2];
//                            }

//                            p += 3;
//                        }
//                        p += nOffset;
//                    }
//                }

//                baseMap.UnlockBits(bitmapData);
//                bSrc.UnlockBits(bmSrc);
//                bSrc.Dispose();
//            }
//        }
//    }
//}
