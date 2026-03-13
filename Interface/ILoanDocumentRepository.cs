using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface ILoanDocumentRepository
{
    Task<UploadServiceResult> SaveAsync(
        UploadRequestContext request,
        IReadOnlyList<PreparedUploadFile> preparedFiles,
        CancellationToken ct);
}
