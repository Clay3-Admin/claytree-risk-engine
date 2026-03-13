using Claytree.Risk.Functions.Infrastructure;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface IUploadService
{
    Task<UploadServiceResult> ProcessAsync(
        UploadRequestContext request,
        IReadOnlyList<MultipartFormDataReader.FilePart> fileParts,
        CancellationToken ct);
}
