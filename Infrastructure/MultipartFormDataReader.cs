using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Claytree.Risk.Functions.Infrastructure;

public static class MultipartFormDataReader
{
    public sealed record FilePart(string FileName, string ContentType, Stream Content);

    public static async Task<FilePart?> ReadFirstFileAsync(Stream body, string contentType, CancellationToken ct)
    {
        var files = await ReadAllFilesAsync(body, contentType, ct);
        return files.FirstOrDefault();
    }

    public static async Task<List<FilePart>> ReadAllFilesAsync(Stream body, string contentType, CancellationToken ct)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            throw new InvalidOperationException("Invalid Content-Type header.");

        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
            throw new InvalidOperationException("Missing multipart boundary.");

        var reader = new MultipartReader(boundary, body);
        var files = new List<FilePart>();

        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync(ct)) is not null)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var cd))
                continue;

            var isFile = string.Equals(cd.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase) &&
                         (!string.IsNullOrEmpty(cd.FileName.Value) || !string.IsNullOrEmpty(cd.FileNameStar.Value));

            if (!isFile)
                continue;

            var fileName = cd.FileNameStar.HasValue ? cd.FileNameStar.Value : cd.FileName.Value;
            fileName = (fileName ?? "upload.bin").Trim('"');

            var ctHeader = string.IsNullOrWhiteSpace(section.ContentType)
                ? "application/octet-stream"
                : section.ContentType;

            var ms = new MemoryStream();
            await section.Body.CopyToAsync(ms, ct);
            ms.Position = 0;

            files.Add(new FilePart(fileName, ctHeader, ms));
        }

        return files;
    }
}