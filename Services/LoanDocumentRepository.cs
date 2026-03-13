using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Net;

namespace Claytree.Risk.Functions.Services;

public sealed class LoanDocumentRepository : ILoanDocumentRepository
{
    private readonly SqlOptions _sqlOptions;
    private readonly BlobOptions _blobOptions;
    private readonly ILoanRepository _loanRepository;
    private readonly IApplicationNumberService _applicationNumberService;
    private readonly ILoanDocumentFingerprintRepository _loanDocumentFingerprintRepository;
    private readonly IBlobStorageService _blobStorageService;

    public LoanDocumentRepository(
        IOptions<SqlOptions> sqlOptions,
        IOptions<BlobOptions> blobOptions,
        ILoanRepository loanRepository,
        IApplicationNumberService applicationNumberService,
        ILoanDocumentFingerprintRepository loanDocumentFingerprintRepository,
        IBlobStorageService blobStorageService)
    {
        _sqlOptions = sqlOptions.Value;
        _blobOptions = blobOptions.Value;
        _loanRepository = loanRepository;
        _applicationNumberService = applicationNumberService;
        _loanDocumentFingerprintRepository = loanDocumentFingerprintRepository;
        _blobStorageService = blobStorageService;
    }

    public async Task<UploadServiceResult> SaveAsync(
        UploadRequestContext request,
        IReadOnlyList<PreparedUploadFile> preparedFiles,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_sqlOptions.ConnectionString))
            throw new UploadWorkflowException(HttpStatusCode.InternalServerError, new ApiError("sql_not_configured", "SQL__ConnectionString is missing."));

        if (string.IsNullOrWhiteSpace(_blobOptions.ContainerName))
            throw new UploadWorkflowException(HttpStatusCode.InternalServerError, new ApiError("blob_not_configured", "BLOB__ContainerName is missing."));

        var loanApplicationId = request.LoanApplicationId;
        var finalApplicationNumber = request.ApplicationNumber;
        var blobContainer = _blobOptions.ContainerName;

        try
        {
            return await ExecuteWithSqlRetryAsync(async retryCt =>
            {
                await using var cn = new SqlConnection(SqlHelper.FixSqlConnectionString(_sqlOptions.ConnectionString));
                await cn.OpenAsync(retryCt);

                await using var tx = await cn.BeginTransactionAsync(retryCt);
                var stx = (SqlTransaction)tx;
                var uploaded = new List<(string container, string path)>();
                var documents = new List<object>();

                try
                {
                    if (loanApplicationId == Guid.Empty)
                    {
                        loanApplicationId = await _loanRepository.InsertLoanApplicationAsync(
                            cn,
                            stx,
                            string.IsNullOrWhiteSpace(request.ApplicationNumber) ? null : request.ApplicationNumber,
                            request.ApplicantName,
                            request.Mobile,
                            request.Email,
                            request.TenantId,
                            request.CreatedBy,
                            retryCt);
                    }
                    else
                    {
                        var exists = await _loanRepository.LoanApplicationExistsAsync(cn, stx, loanApplicationId, retryCt);
                        if (!exists)
                            throw new UploadWorkflowException(HttpStatusCode.BadRequest, new ApiError("validation_failed", "loanApplicationId not found."));
                    }

                    finalApplicationNumber = await _applicationNumberService.EnsureAsync(
                        cn,
                        stx,
                        loanApplicationId,
                        request.TenantId,
                        request.BranchCode,
                        request.ProductCode,
                        request.ApplicationNumber,
                        retryCt);

                    foreach (var pf in preparedFiles)
                    {
                        if (pf.Content.CanSeek)
                            pf.Content.Position = 0;

                        var uploadResult = await _blobStorageService.UploadAsync(
                            pf.Content,
                            pf.FileName,
                            pf.ContentType,
                            retryCt);

                        uploaded.Add((blobContainer, uploadResult.BlobName));

                        var loanDocumentId = await _loanRepository.InsertLoanDocumentAsync(
                            cn,
                            stx,
                            loanApplicationId,
                            request.DocumentType,
                            pf.FileName,
                            pf.ContentType,
                            uploadResult.SizeBytes,
                            blobContainer,
                            uploadResult.BlobName,
                            uploadResult.BlobUrl,
                            request.CreatedBy,
                            declaredDocumentType: NullIfWhiteSpace(request.DeclaredDocumentType),
                            documentOwnerRole: NullIfWhiteSpace(request.DocumentOwnerRole),
                            documentSide: NullIfWhiteSpace(request.DocumentSide),
                            captureType: NullIfWhiteSpace(request.CaptureType),
                            languageHint: NullIfWhiteSpace(request.LanguageHint),
                            source: NullIfWhiteSpace(request.Source),
                            replacesLoanDocumentId: request.ReplacesLoanDocumentId,
                            ct: retryCt);

                        await _loanDocumentFingerprintRepository.SaveFingerprintAsync(
                            cn,
                            stx,
                            loanDocumentId,
                            loanApplicationId,
                            request.TenantId,
                            pf.Fingerprint,
                            retryCt);

                        documents.Add(new
                        {
                            loanDocumentId,
                            originalFileName = pf.FileName,
                            contentType = pf.ContentType,
                            sizeBytes = uploadResult.SizeBytes,
                            validation = new
                            {
                                isValid = pf.Validation.IsValid,
                                pageCount = pf.Validation.PageCount,
                                widthPx = pf.Validation.WidthPx,
                                heightPx = pf.Validation.HeightPx,
                                horizontalDpi = pf.Validation.HorizontalDpi,
                                verticalDpi = pf.Validation.VerticalDpi,
                                errors = pf.Validation.Errors,
                                warnings = pf.Validation.Warnings
                            },
                            security = new
                            {
                                isSafe = pf.SecurityValidation.IsSafe,
                                detectedFileType = pf.SecurityValidation.DetectedFileType,
                                signatureMatched = pf.SecurityValidation.SignatureMatched,
                                isEncrypted = pf.SecurityValidation.IsEncrypted,
                                hasMacros = pf.SecurityValidation.HasMacros,
                                errors = pf.SecurityValidation.Errors,
                                warnings = pf.SecurityValidation.Warnings
                            },
                            fingerprint = new
                            {
                                sha256Hash = pf.Fingerprint.Sha256Hash,
                                contentNormalizedHash = pf.Fingerprint.ContentNormalizedHash,
                                perceptualHash = pf.Fingerprint.PerceptualHash,
                                pageCount = pf.Fingerprint.PageCount,
                                widthPx = pf.Fingerprint.WidthPx,
                                heightPx = pf.Fingerprint.HeightPx,
                                warnings = pf.Fingerprint.Warnings
                            },
                            duplicateDetection = new
                            {
                                isDuplicate = pf.DuplicateDetection.IsDuplicate,
                                isNearDuplicate = pf.DuplicateDetection.IsNearDuplicate,
                                duplicateType = pf.DuplicateDetection.DuplicateType,
                                matchedLoanDocumentId = pf.DuplicateDetection.MatchedLoanDocumentId,
                                matchedLoanApplicationId = pf.DuplicateDetection.MatchedLoanApplicationId,
                                matchReason = pf.DuplicateDetection.MatchReason
                            },
                            blob = new
                            {
                                blobContainer,
                                blobPath = uploadResult.BlobName,
                                blobUrl = uploadResult.BlobUrl
                            }
                        });
                    }

                    await tx.CommitAsync(retryCt);

                    return new UploadServiceResult
                    {
                        Message = "Files validated, fingerprinted, uploaded and saved to SQL",
                        LoanApplicationId = loanApplicationId,
                        ApplicationNumber = finalApplicationNumber,
                        Documents = documents
                    };
                }
                catch
                {
                    await tx.RollbackAsync(retryCt);

                    foreach (var b in uploaded)
                    {
                        try { await _blobStorageService.DeleteAsync(b.container, b.path, retryCt); }
                        catch { }
                    }

                    throw;
                }
            }, ct);
        }
        catch (UploadWorkflowException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new UploadWorkflowException(HttpStatusCode.InternalServerError, new ApiError("sql_error", ex.Message));
        }
    }

    

    private static async Task<T> ExecuteWithSqlRetryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        var delays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7) };

        Exception? last = null;
        for (var i = 0; i < delays.Length + 1; i++)
        {
            try
            {
                return await action(ct);
            }
            catch (UploadWorkflowException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                last = ex;
            }
            catch (InvalidOperationException ex)
            {
                last = ex;
            }

            if (i < delays.Length)
                await Task.Delay(delays[i], ct);
        }

        throw last ?? new Exception("SQL operation failed.");
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
