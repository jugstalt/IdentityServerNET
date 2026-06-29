#nullable enable

using Microsoft.AspNetCore.Http;
using SkiaSharp;
using System.IO;
using System.Threading.Tasks;

namespace IdentityServer.Net.Services;

/// <summary>
/// Validates that an uploaded file is a genuine raster image (PNG, JPEG or WebP).
/// SVG is intentionally rejected — it can contain scripts and event handlers.
/// Validation strategy: magic-byte check → SkiaSharp decode attempt.
/// Returns the file bytes on success so the caller does not need to re-read the stream.
/// </summary>
public static class ImageUploadValidator
{
    private const int MaxFileSizeBytes = 512 * 1024; // 512 KB

    // Known raster image magic bytes
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] WebpMagic = { 0x52, 0x49, 0x46, 0x46 }; // RIFF....WEBP

    /// <summary>
    /// Validates the uploaded image. On success, <c>bytes</c> contains the file data
    /// ready for storage — the caller must not read the IFormFile stream again.
    /// On failure, <c>bytes</c> is null and <c>error</c> contains the user-facing message.
    /// </summary>
    public static async Task<(bool valid, string error, byte[]? bytes)> ValidateAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return (false, "No file provided.", null);

        if (file.Length > MaxFileSizeBytes)
            return (false, $"File too large. Maximum size is {MaxFileSizeBytes / 1024} KB.", null);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        if (!HasKnownImageMagicBytes(bytes))
            return (false, "Unsupported file type. Only PNG, JPEG and WebP images are accepted. SVG is not supported.", null);

        // Actually decode with SkiaSharp — catches corrupted files and polyglots
        using var skData = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(skData);
        if (codec is null)
            return (false, "The file could not be decoded as a valid image.", null);

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height);
        if (info.Width <= 0 || info.Height <= 0 || info.Width > 4096 || info.Height > 4096)
            return (false, "Image dimensions are invalid or too large (max 4096×4096 px).", null);

        using var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());
        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
            return (false, "The image data is corrupted or incomplete.", null);

        return (true, string.Empty, bytes);
    }

    private static bool HasKnownImageMagicBytes(byte[] bytes)
    {
        if (bytes.Length < 12) return false;

        if (StartsWith(bytes, PngMagic)) return true;
        if (StartsWith(bytes, JpegMagic)) return true;

        // WebP: RIFF????WEBP
        if (StartsWith(bytes, WebpMagic) &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return true;

        return false;
    }

    private static bool StartsWith(byte[] data, byte[] magic)
    {
        if (data.Length < magic.Length) return false;
        for (int i = 0; i < magic.Length; i++)
            if (data[i] != magic[i]) return false;
        return true;
    }
}
