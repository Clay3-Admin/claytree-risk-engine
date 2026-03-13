using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Services;

public sealed class SecurityFileValidator : ISecurityFileValidator
{
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".msi", ".js", ".vbs", ".ps1", ".scr", ".com",
        ".zip", ".rar", ".7z", ".iso"
    };

    public async Task<FileSecurityValidationResult> ValidateAsync(
        string fileName,
        string? contentType,
        Stream fileStream,
        CancellationToken ct)
    {
        var result = new FileSecurityValidationResult
        {
            FileName = fileName ?? string.Empty,
            Extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant(),
            DeclaredContentType = contentType ?? string.Empty,
            IsSafe = true
        };

        if (fileStream == null || !fileStream.CanRead)
        {
            result.IsSafe = false;
            result.Errors.Add("File stream is null or unreadable.");
            return result;
        }

        if (BlockedExtensions.Contains(result.Extension))
        {
            result.IsSafe = false;
            result.Errors.Add($"Blocked file type '{result.Extension}'.");
            return result;
        }

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var header = await ReadHeaderAsync(fileStream, 32, ct);

        result.DetectedFileType = DetectFileType(header);
        result.SignatureMatched = SignatureMatchesExtension(result.Extension, result.DetectedFileType);

        if (!result.SignatureMatched)
        {
            result.IsSafe = false;
            result.Errors.Add(
                $"File signature does not match extension. Extension '{result.Extension}', detected '{result.DetectedFileType ?? "unknown"}'.");
        }

        if (result.DetectedFileType is "zip" or "rar" or "7z" or "exe")
        {
            result.IsSafe = false;
            result.Errors.Add($"Detected blocked binary type '{result.DetectedFileType}'.");
        }

        if (result.Extension == ".pdf")
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            var pdfText = await ReadAsciiPrefixAsync(fileStream, 4096, ct);

            if (pdfText.Contains("/Encrypt", StringComparison.OrdinalIgnoreCase))
            {
                result.IsEncrypted = true;
                result.IsSafe = false;
                result.Errors.Add("Encrypted/password-protected PDF is not allowed.");
            }
        }

        if (result.Extension is ".docm" or ".xlsm" or ".pptm")
        {
            result.HasMacros = true;
            result.IsSafe = false;
            result.Errors.Add("Macro-enabled Office documents are not allowed.");
        }

        return result;
    }

    private static async Task<byte[]> ReadHeaderAsync(Stream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];

        if (stream.CanSeek)
            stream.Position = 0;

        var read = await stream.ReadAsync(buffer.AsMemory(0, count), ct);

        if (stream.CanSeek)
            stream.Position = 0;

        return buffer.Take(read).ToArray();
    }

    private static async Task<string> ReadAsciiPrefixAsync(Stream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];

        if (stream.CanSeek)
            stream.Position = 0;

        var read = await stream.ReadAsync(buffer.AsMemory(0, count), ct);

        if (stream.CanSeek)
            stream.Position = 0;

        return System.Text.Encoding.ASCII.GetString(buffer, 0, read);
    }

    private static string? DetectFileType(byte[] header)
    {
        if (StartsWith(header, 0x25, 0x50, 0x44, 0x46)) return "pdf";     // %PDF
        if (StartsWith(header, 0xFF, 0xD8, 0xFF)) return "jpeg";
        if (StartsWith(header, 0x89, 0x50, 0x4E, 0x47)) return "png";
        if (StartsWith(header, 0x42, 0x4D)) return "bmp";
        if (StartsWith(header, 0x49, 0x49, 0x2A, 0x00) || StartsWith(header, 0x4D, 0x4D, 0x00, 0x2A)) return "tiff";
        if (StartsWith(header, 0x52, 0x49, 0x46, 0x46) && header.Length >= 12 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return "webp";
        if (StartsWith(header, 0x50, 0x4B, 0x03, 0x04)) return "zip";
        if (StartsWith(header, 0x52, 0x61, 0x72, 0x21)) return "rar";
        if (StartsWith(header, 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C)) return "7z";
        if (StartsWith(header, 0x4D, 0x5A)) return "exe";                 // MZ

        return "unknown";
    }

    private static bool SignatureMatchesExtension(string extension, string? detected)
    {
        return extension switch
        {
            ".pdf" => detected == "pdf",
            ".jpg" or ".jpeg" => detected == "jpeg",
            ".png" => detected == "png",
            ".bmp" => detected == "bmp",
            ".tif" or ".tiff" => detected == "tiff",
            ".webp" => detected == "webp",
            _ => detected != "exe" && detected != "zip" && detected != "rar" && detected != "7z"
        };
    }

    private static bool StartsWith(byte[] buffer, params byte[] signature)
    {
        if (buffer.Length < signature.Length) return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i]) return false;
        }

        return true;
    }
}