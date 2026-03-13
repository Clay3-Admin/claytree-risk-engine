using Claytree.Risk.Functions.Infrastructure;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface IValidationService
{
    Task<List<PreparedUploadFile>> PrepareAsync(
        IReadOnlyList<MultipartFormDataReader.FilePart> fileParts,
        CancellationToken ct);
}
