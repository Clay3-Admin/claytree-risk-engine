using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface IFileStructuralValidator
{
    Task<FileValidationResult> ValidateAsync(
        string fileName,
        string? contentType,
        Stream fileStream,
        CancellationToken ct);
}