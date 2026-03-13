using Claytree.Risk.Functions.Infrastructure;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Options;
using Microsoft.Extensions.Options;
using System.Net;

namespace Claytree.Risk.Functions.Services;

public sealed class ValidationService : IValidationService
{
    private readonly UploadOptions _uploadOptions;
    private readonly IFileStructuralValidator _fileStructuralValidator;
    private readonly ISecurityFileValidator _securityFileValidator;

    public ValidationService(
        IOptions<UploadOptions> uploadOptions,
        IFileStructuralValidator fileStructuralValidator,
        ISecurityFileValidator securityFileValidator)
    {
        _uploadOptions = uploadOptions.Value;
        _fileStructuralValidator = fileStructuralValidator;
        _securityFileValidator = securityFileValidator;
    }

    public async Task<List<PreparedUploadFile>> PrepareAsync(
        IReadOnlyList<MultipartFormDataReader.FilePart> fileParts,
        CancellationToken ct)
    {
        var preparedFiles = new List<PreparedUploadFile>();
        var maxBytes = _uploadOptions.MaxFileMb * 1024L * 1024L;

        try
        {
            foreach (var fp in fileParts)
            {
                var ext = Path.GetExtension(fp.FileName).ToLowerInvariant();

                if (!_uploadOptions.AllowedExtensions.Contains(ext))
                {
                    throw new UploadWorkflowException(HttpStatusCode.UnprocessableEntity, new
                    {
                        message = "One or more files failed validation.",
                        documentCount = fileParts.Count,
                        files = new[]
                        {
                            new
                            {
                                fileName = fp.FileName,
                                contentType = fp.ContentType,
                                extension = ext,
                                isValid = false,
                                errors = new[]
                                {
                                    $"Invalid file type '{ext}' for file '{fp.FileName}'. Allowed: {string.Join(", ", _uploadOptions.AllowedExtensions)}"
                                }
                            }
                        }
                    });
                }

                var ms = new MemoryStream();

                if (fp.Content.CanSeek)
                    fp.Content.Position = 0;

                await CopyWithLimitAsync(fp.Content, ms, maxBytes, ct);
                ms.Position = 0;

                var validation = await _fileStructuralValidator.ValidateAsync(fp.FileName, fp.ContentType, ms, ct);
                ms.Position = 0;

                var securityValidation = await _securityFileValidator.ValidateAsync(fp.FileName, fp.ContentType, ms, ct);
                ms.Position = 0;

                preparedFiles.Add(new PreparedUploadFile
                {
                    FileName = fp.FileName,
                    ContentType = fp.ContentType,
                    Content = ms,
                    Validation = validation,
                    SecurityValidation = securityValidation
                });
            }

            var invalidFiles = preparedFiles
                .Where(x => !x.Validation.IsValid)
                .Select(x => new
                {
                    fileName = x.Validation.FileName,
                    contentType = x.Validation.ContentType,
                    extension = x.Validation.Extension,
                    fileSizeBytes = x.Validation.FileSizeBytes,
                    isValid = x.Validation.IsValid,
                    pageCount = x.Validation.PageCount,
                    widthPx = x.Validation.WidthPx,
                    heightPx = x.Validation.HeightPx,
                    horizontalDpi = x.Validation.HorizontalDpi,
                    verticalDpi = x.Validation.VerticalDpi,
                    errors = x.Validation.Errors,
                    warnings = x.Validation.Warnings
                })
                .ToList();

            if (invalidFiles.Count > 0)
            {
                throw new UploadWorkflowException(HttpStatusCode.UnprocessableEntity, new
                {
                    message = "One or more files failed structural validation. Upload aborted.",
                    documentCount = fileParts.Count,
                    validFileCount = preparedFiles.Count - invalidFiles.Count,
                    invalidFileCount = invalidFiles.Count,
                    files = invalidFiles
                });
            }

            var unsafeFiles = preparedFiles
                .Where(x => !x.SecurityValidation.IsSafe)
                .Select(x => new
                {
                    fileName = x.SecurityValidation.FileName,
                    extension = x.SecurityValidation.Extension,
                    declaredContentType = x.SecurityValidation.DeclaredContentType,
                    detectedFileType = x.SecurityValidation.DetectedFileType,
                    signatureMatched = x.SecurityValidation.SignatureMatched,
                    isEncrypted = x.SecurityValidation.IsEncrypted,
                    hasMacros = x.SecurityValidation.HasMacros,
                    errors = x.SecurityValidation.Errors,
                    warnings = x.SecurityValidation.Warnings
                })
                .ToList();

            if (unsafeFiles.Count > 0)
            {
                throw new UploadWorkflowException(HttpStatusCode.UnprocessableEntity, new
                {
                    message = "One or more files failed security validation. Upload aborted.",
                    documentCount = fileParts.Count,
                    safeFileCount = preparedFiles.Count - unsafeFiles.Count,
                    unsafeFileCount = unsafeFiles.Count,
                    files = unsafeFiles
                });
            }

            return preparedFiles;
        }
        catch
        {
            foreach (var file in preparedFiles)
            {
                file.Content.Dispose();
            }

            throw;
        }
    }

    private static async Task CopyWithLimitAsync(Stream src, Stream dst, long maxBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;

        while (true)
        {
            var read = await src.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read <= 0)
                break;

            total += read;
            if (total > maxBytes)
            {
                throw new UploadWorkflowException(
                    HttpStatusCode.BadRequest,
                    new ApiError("validation_failed", $"File too large. Max allowed is {maxBytes / (1024 * 1024)} MB."));
            }

            await dst.WriteAsync(buffer, 0, read, ct);
        }
    }
}
