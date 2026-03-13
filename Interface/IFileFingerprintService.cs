using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface IFileFingerprintService
{
    Task<FileFingerprintResult> GenerateAsync(
        string fileName,
        string? contentType,
        Stream fileStream,
        CancellationToken ct);
}