using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using UglyToad.PdfPig;
using Claytree.Risk.Functions.Interface;

namespace Claytree.Risk.Functions.Services;

public sealed class FileStructuralValidator : IFileStructuralValidator
{
    private readonly FileValidationOptions _options;

    public FileStructuralValidator(IOptions<FileValidationOptions> options)
    {
        _options = options.Value;
    }

    public async Task<FileValidationResult> ValidateAsync(
        string fileName,
        string? contentType,
        Stream fileStream,
        CancellationToken ct)
    {
        if (fileStream == null || !fileStream.CanRead)
        {
            return new FileValidationResult
            {
                FileName = fileName ?? "",
                ContentType = contentType ?? "",
                Extension = Path.GetExtension(fileName ?? "").ToLowerInvariant(),
                IsValid = false,
                Errors = { "File stream is null or unreadable." }
            };
        }

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var size = fileStream.CanSeek ? fileStream.Length : 0;

        var result = new FileValidationResult
        {
            FileName = fileName ?? "",
            ContentType = contentType ?? "",
            Extension = Path.GetExtension(fileName ?? "").ToLowerInvariant(),
            FileSizeBytes = size,
            IsValid = true
        };

        if (size <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Uploaded file is empty.");
            return result;
        }

        ValidateExtension(result);
        ValidateFileSize(result);

        if (!result.IsValid)
            return result;

        if (result.Extension == ".pdf")
        {
            ValidatePdf(result, fileStream);
            return result;
        }

        if (IsImage(result.Extension))
        {
            await ValidateImageAsync(result, fileStream, ct);
            return result;
        }

        return result;
    }

    private void ValidateExtension(FileValidationResult result)
    {
        if (_options.AllowedExtensions.Count == 0)
            return;

        if (!_options.AllowedExtensions.Contains(result.Extension))
        {
            result.IsValid = false;
            result.Errors.Add(
                $"Invalid file type '{result.Extension}'. Allowed: {string.Join(", ", _options.AllowedExtensions)}");
        }
    }

    private void ValidateFileSize(FileValidationResult result)
    {
        var maxBytes = _options.MaxFileSizeMb * 1024L * 1024L;

        if (result.FileSizeBytes > maxBytes)
        {
            result.IsValid = false;
            result.Errors.Add(
                $"File too large. Size is {result.FileSizeBytes} bytes. Max allowed is {_options.MaxFileSizeMb} MB.");
        }
    }

    private void ValidatePdf(FileValidationResult result, Stream stream)
    {
        try
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using var pdf = PdfDocument.Open(stream);

            result.PageCount = pdf.NumberOfPages;

            if (result.PageCount > _options.MaxPdfPages)
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"PDF page count {result.PageCount} exceeds maximum allowed {_options.MaxPdfPages}.");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Unable to read PDF structure. {ex.Message}");
        }
    }

    private async Task ValidateImageAsync(FileValidationResult result, Stream stream, CancellationToken ct)
    {
        try
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using var image = await Image.LoadAsync(stream, ct);

            result.WidthPx = image.Width;
            result.HeightPx = image.Height;

            var horizontalDpi = image.Metadata.HorizontalResolution;
            var verticalDpi = image.Metadata.VerticalResolution;

            result.HorizontalDpi = horizontalDpi > 0 ? horizontalDpi : null;
            result.VerticalDpi = verticalDpi > 0 ? verticalDpi : null;

            // HARD validation: dimensions

            if (result.WidthPx < _options.MinWidthPx || result.HeightPx < _options.MinHeightPx)
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"Image dimensions {result.WidthPx}x{result.HeightPx} are below minimum allowed {_options.MinWidthPx}x{_options.MinHeightPx}.");
            }

            if (result.WidthPx > _options.MaxWidthPx || result.HeightPx > _options.MaxHeightPx)
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"Image dimensions {result.WidthPx}x{result.HeightPx} exceed maximum allowed {_options.MaxWidthPx}x{_options.MaxHeightPx}.");
            }

            // SOFT validation: DPI

            if (!result.HorizontalDpi.HasValue || !result.VerticalDpi.HasValue)
            {
                result.Warnings.Add("Image DPI metadata missing.");
                return;
            }

            if (result.HorizontalDpi < _options.MinDpi || result.VerticalDpi < _options.MinDpi)
            {
                result.Warnings.Add(
                    $"Low DPI detected ({result.HorizontalDpi}x{result.VerticalDpi}). Recommended minimum is {_options.MinDpi}.");
            }

            if (result.HorizontalDpi > _options.MaxDpi || result.VerticalDpi > _options.MaxDpi)
            {
                result.Warnings.Add(
                    $"Very high DPI detected ({result.HorizontalDpi}x{result.VerticalDpi}). Recommended maximum is {_options.MaxDpi}.");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Unable to read image structure. {ex.Message}");
        }
    }

    private static bool IsImage(string extension)
    {
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".tif" or ".tiff" or ".webp";
    }
}