using Claytree.Risk.Functions.Infrastructure;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using System.Net;

namespace Claytree.Risk.Functions.Services;

public sealed class UploadService : IUploadService
{
    private readonly IValidationService _validationService;
    private readonly IFileFingerprintService _fileFingerprintService;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly ILoanDocumentRepository _loanDocumentRepository;

    public UploadService(
        IValidationService validationService,
        IFileFingerprintService fileFingerprintService,
        IDuplicateDetectionService duplicateDetectionService,
        ILoanDocumentRepository loanDocumentRepository)
    {
        _validationService = validationService;
        _fileFingerprintService = fileFingerprintService;
        _duplicateDetectionService = duplicateDetectionService;
        _loanDocumentRepository = loanDocumentRepository;
    }

    public async Task<UploadServiceResult> ProcessAsync(
        UploadRequestContext request,
        IReadOnlyList<MultipartFormDataReader.FilePart> fileParts,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            throw new UploadWorkflowException(
                HttpStatusCode.BadRequest,
                new ApiError("documentType_missing", "documentType is required to save SQL entry."));
        }

        List<PreparedUploadFile>? preparedFiles = null;

        try
        {
            preparedFiles = await _validationService.PrepareAsync(fileParts, ct);

            foreach (var pf in preparedFiles)
            {
                if (pf.Content.CanSeek)
                    pf.Content.Position = 0;

                pf.Fingerprint = await _fileFingerprintService.GenerateAsync(
                    pf.FileName,
                    pf.ContentType,
                    pf.Content,
                    ct);

                if (pf.Content.CanSeek)
                    pf.Content.Position = 0;

                pf.DuplicateDetection = await _duplicateDetectionService.CheckAsync(
                    pf.Fingerprint,
                    request.TenantId,
                    ct);
            }

            var duplicateFiles = preparedFiles
                .Where(x => x.DuplicateDetection.IsDuplicate)
                .Select(x => new
                {
                    fileName = x.FileName,
                    duplicateType = x.DuplicateDetection.DuplicateType,
                    matchedLoanDocumentId = x.DuplicateDetection.MatchedLoanDocumentId,
                    matchedLoanApplicationId = x.DuplicateDetection.MatchedLoanApplicationId,
                    matchReason = x.DuplicateDetection.MatchReason,
                    sha256Hash = x.Fingerprint.Sha256Hash
                })
                .ToList();

            if (duplicateFiles.Count > 0)
            {
                throw new UploadWorkflowException(HttpStatusCode.Conflict, new
                {
                    message = "One or more files are duplicates. Upload aborted.",
                    duplicateFileCount = duplicateFiles.Count,
                    files = duplicateFiles
                });
            }

            return await _loanDocumentRepository.SaveAsync(request, preparedFiles, ct);
        }
        finally
        {
            if (preparedFiles is null)
            {
                // Nothing to dispose.
            }
            else
            {
                foreach (var file in preparedFiles)
                {
                    file.Content.Dispose();
                }
            }
        }
    }
}
