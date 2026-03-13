using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;
using System.Text;
using UglyToad.PdfPig;
using System.Linq;
using System.Collections.Generic;


namespace Claytree.Risk.Functions.Services;

public sealed class FileFingerprintService : IFileFingerprintService
{
    public async Task<FileFingerprintResult> GenerateAsync(
        string fileName,
        string? contentType,
        Stream fileStream,
        CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var sha256 = await ComputeSha256Async(fileStream, ct);

        var result = new FileFingerprintResult
        {
            FileName = fileName ?? string.Empty,
            Extension = ext,
            Sha256Hash = sha256,
            FileSizeBytes = fileStream.CanSeek ? fileStream.Length : 0
        };

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        if (ext == ".pdf")
        {
            await PopulatePdfFingerprintAsync(result, fileStream, ct);
        }
        else if (ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".tif" or ".tiff" or ".webp")
        {
            await PopulateImageFingerprintAsync(result, fileStream, ct);
        }

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        return result;
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);

        if (stream.CanSeek)
            stream.Position = 0;

        return Convert.ToHexString(hash);
    }

    private static Task PopulatePdfFingerprintAsync(FileFingerprintResult result, Stream stream, CancellationToken ct)
    {
        try
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using var pdf = PdfDocument.Open(stream);
            result.PageCount = pdf.NumberOfPages;

            var sb = new StringBuilder();

            foreach (var page in pdf.GetPages().Take(5))
            {
                var text = page.Text ?? string.Empty;
                var normalized = NormalizeText(text);
                sb.Append(normalized);
            }

            var normalizedText = sb.ToString();
            if (!string.IsNullOrWhiteSpace(normalizedText))
            {
                result.ContentNormalizedHash = ComputeSha256FromString(normalizedText);
            }
            else
            {
                result.Warnings.Add("PDF text normalization unavailable; OCR-based duplicate detection may be needed later.");
            }
        }
        catch
        {
            result.Warnings.Add("Unable to generate PDF normalized fingerprint.");
        }
        finally
        {
            if (stream.CanSeek)
                stream.Position = 0;
        }

        return Task.CompletedTask;
    }

    private static async Task PopulateImageFingerprintAsync(
     FileFingerprintResult result,
     Stream stream,
     CancellationToken ct)
    {
        try
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using var image = await Image.LoadAsync<Rgba32>(stream, ct);

            result.WidthPx = image.Width;
            result.HeightPx = image.Height;

            using var clone = image.CloneAs<Rgba32>();
            clone.Mutate(x => x.Resize(8, 8).Grayscale());

            var values = new List<byte>(64);

            for (int y = 0; y < clone.Height; y++)
            {
                for (int x = 0; x < clone.Width; x++)
                {
                    var pixel = clone[x, y];
                    values.Add(pixel.R);
                }
            }

            var avg = values.Average(v => (double)v);
            var bits = string.Concat(values.Select(v => v >= avg ? "1" : "0"));
            result.PerceptualHash = ConvertBitsToHex(bits);
        }
        catch
        {
            result.Warnings.Add("Unable to generate image perceptual fingerprint.");
        }
        finally
        {
            if (stream.CanSeek)
                stream.Position = 0;
        }
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var chars = text
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray();

        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ComputeSha256FromString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string ConvertBitsToHex(string bits)
    {
        var bytes = new byte[bits.Length / 8];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(bits.Substring(i * 8, 8), 2);
        }

        return Convert.ToHexString(bytes);
    }
}