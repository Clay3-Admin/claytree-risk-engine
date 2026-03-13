using System.Net;
using Claytree.Risk.Functions.Extensions;
using Claytree.Risk.Functions.Infrastructure;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Claytree.Risk.Functions.Functions;

public sealed class UploadFileFunction
{
    private readonly ILogger<UploadFileFunction> _logger;
    private readonly IBlobUploader _uploader;
    private readonly UploadOptions _uploadOptions;
    private readonly SqlOptions _sqlOptions;
    private readonly BlobOptions _blobOptions;
    private readonly ILoanRepository _loanRepo;
    private readonly IApplicationNumberService _appNoService;

    public UploadFileFunction(
        ILogger<UploadFileFunction> logger,
        IBlobUploader uploader,
        IOptions<UploadOptions> uploadOptions,
        IOptions<SqlOptions> sqlOptions,
        IOptions<BlobOptions> blobOptions,
        ILoanRepository loanRepo,
        IApplicationNumberService appNoService)
    {
        _logger = logger;
        _uploader = uploader;
        _uploadOptions = uploadOptions.Value;
        _sqlOptions = sqlOptions.Value;
        _blobOptions = blobOptions.Value;
        _loanRepo = loanRepo;
        _appNoService = appNoService;
    }

    [Function("upload-file")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload")] HttpRequestData req,
        FunctionContext ctx)
    {
        var res = req.CreateResponse();
        var ct = ctx.CancellationToken;

        try
        {
            // 1) Validate multipart
            var contentType = req.Headers.TryGetValues("Content-Type", out var values)
                ? values.FirstOrDefault() ?? ""
                : "";

            if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                return res.WriteError(HttpStatusCode.BadRequest, "invalid_content_type", "Expected multipart/form-data.");

            // ✅ Read ALL files
            var fileParts = await MultipartFormDataReader.ReadAllFilesAsync(req.Body, contentType, ct);
            if (fileParts.Count == 0)
                return res.WriteError(HttpStatusCode.BadRequest, "no_file", "No file found in multipart request.");

            var maxBytes = _uploadOptions.MaxFileMb * 1024L * 1024L;

            // 2) Validate extensions for all files up-front
            foreach (var fp in fileParts)
            {
                var ext = Path.GetExtension(fp.FileName).ToLowerInvariant();
                if (!_uploadOptions.AllowedExtensions.Contains(ext))
                    return res.WriteError(HttpStatusCode.BadRequest, "invalid_file_type",
                        $"Invalid file type '{ext}' for file '{fp.FileName}'. Allowed: {string.Join(", ", _uploadOptions.AllowedExtensions)}");
            }

            // 3) Read query params
            var q = ParseQuery(req.Url);
            string GetQ(string key) => q.TryGetValue(key, out var v) ? v : "";

            var documentType = GetQ("documentType").Trim();
            var applicationNumber = GetQ("applicationNumber");
            var applicantName = GetQ("applicantName");
            var mobile = GetQ("mobile");
            var email = GetQ("email");
            var createdBy = GetQ("createdBy");

            var branchCode = GetQ("branchCode");
            var productCode = GetQ("productCode");

            // NEW optional metadata (applies to all files in this request)
            var declaredDocumentType = GetQ("declaredDocumentType").Trim();
            var documentOwnerRole = GetQ("documentOwnerRole").Trim();   // Applicant/CoApplicant/Guarantor/Business
            var documentSide = GetQ("documentSide").Trim();        // Front/Back
            var captureType = GetQ("captureType").Trim();         // camera/scan/screenshot/downloaded
            var languageHint = GetQ("languageHint").Trim();        // en/hi/ta/te...
            var source = GetQ("source").Trim();              // API/UI/Partner/Manual

            Guid? replacesLoanDocumentId = null;
            if (Guid.TryParse(GetQ("replacesLoanDocumentId"), out var rid))
                replacesLoanDocumentId = rid;

            Guid.TryParse(GetQ("loanApplicationId"), out var loanApplicationId);
            Guid? tenantId = Guid.TryParse(GetQ("tenantId"), out var tid) ? tid : null;

            if (string.IsNullOrWhiteSpace(documentType))
                return res.WriteError(HttpStatusCode.BadRequest, "documentType_missing", "documentType is required to save SQL entry.");

            if (string.IsNullOrWhiteSpace(_sqlOptions.ConnectionString))
                return res.WriteError(HttpStatusCode.InternalServerError, "sql_not_configured", "SQL__ConnectionString is missing.");

            if (string.IsNullOrWhiteSpace(_blobOptions.ContainerName))
                return res.WriteError(HttpStatusCode.InternalServerError, "blob_not_configured", "BLOB__ContainerName is missing.");

            // 4) SQL transaction + retry (all-or-nothing)
            string finalApplicationNumber = applicationNumber;
            var blobContainer = _blobOptions.ContainerName;

            List<object> documents = new();

            try
            {
                (loanApplicationId, finalApplicationNumber, documents) =
                    await ExecuteWithSqlRetryAsync(async retryCt =>
                    {
                        await using var cn = new SqlConnection(FixSqlConnectionString(_sqlOptions.ConnectionString));
                        await cn.OpenAsync(retryCt);

                        await using var tx = await cn.BeginTransactionAsync(retryCt);
                        var stx = (SqlTransaction)tx;

                        // For rollback if anything fails mid-way
                        var uploaded = new List<(string container, string path)>();

                        try
                        {
                            // Create / validate loan application once
                            if (loanApplicationId == Guid.Empty)
                            {
                                loanApplicationId = await _loanRepo.InsertLoanApplicationAsync(
                                    cn, stx,
                                    string.IsNullOrWhiteSpace(applicationNumber) ? null : applicationNumber,
                                    applicantName, mobile, email,
                                    tenantId, createdBy,
                                    retryCt);
                            }
                            else
                            {
                                var exists = await _loanRepo.LoanApplicationExistsAsync(cn, stx, loanApplicationId, retryCt);
                                if (!exists)
                                    throw new InvalidOperationException("loanApplicationId not found.");
                            }

                            // Ensure AppNo once
                            finalApplicationNumber = await _appNoService.EnsureAsync(
                                cn, stx,
                                loanApplicationId, tenantId,
                                branchCode, productCode,
                                applicationNumber,
                                retryCt);

                            // Process each file
                            foreach (var fp in fileParts)
                            {
                                // Copy each file (size-limited) BEFORE moving to next section
                                using var ms = new MemoryStream();
                                await CopyWithLimitAsync(fp.Content, ms, maxBytes, retryCt);
                                ms.Position = 0;

                                // Upload blob
                                var uploadResult = await _uploader.UploadAsync(ms, fp.FileName, fp.ContentType, retryCt);

                                var blobPath = uploadResult.BlobName;
                                var blobUrl = uploadResult.BlobUrl;
                                var fileSizeBytes = uploadResult.SizeBytes;

                                uploaded.Add((blobContainer, blobPath));

                                // Insert LoanDocument
                                var loanDocumentId = await _loanRepo.InsertLoanDocumentAsync(
                                    cn, stx,
                                    loanApplicationId, documentType,
                                    fp.FileName, fp.ContentType,
                                    fileSizeBytes,
                                    blobContainer, blobPath, blobUrl,
                                    createdBy,

    // NEW optional metadata
    declaredDocumentType: string.IsNullOrWhiteSpace(declaredDocumentType) ? null : declaredDocumentType,
    documentOwnerRole: string.IsNullOrWhiteSpace(documentOwnerRole) ? null : documentOwnerRole,
    documentSide: string.IsNullOrWhiteSpace(documentSide) ? null : documentSide,
    captureType: string.IsNullOrWhiteSpace(captureType) ? null : captureType,
    languageHint: string.IsNullOrWhiteSpace(languageHint) ? null : languageHint,
    source: string.IsNullOrWhiteSpace(source) ? null : source,
    replacesLoanDocumentId: replacesLoanDocumentId,
    ct: retryCt
                                    );

                                documents.Add(new
                                {
                                    loanDocumentId,
                                    originalFileName = fp.FileName,
                                    contentType = fp.ContentType,
                                    sizeBytes = fileSizeBytes,
                                    blob = new { blobContainer, blobPath, blobUrl }
                                });
                            }

                            await tx.CommitAsync(retryCt);
                            return (loanApplicationId, finalApplicationNumber, documents);
                        }
                        catch
                        {
                            await tx.RollbackAsync(retryCt);

                            // Rollback blobs best-effort
                            foreach (var b in uploaded)
                            {
                                try { await _uploader.DeleteAsync(b.container, b.path, retryCt); }
                                catch { /* ignore */ }
                            }

                            throw;
                        }
                    }, ct);
            }
            catch (Exception sqlEx)
            {
                _logger.LogError(sqlEx, "SQL save failed");
                return res.WriteError(HttpStatusCode.InternalServerError, "sql_error", sqlEx.Message);
            }

            // 5) Success
            return res.WriteJson(HttpStatusCode.OK, new
            {
                message = "Files uploaded and saved to SQL",
                loanApplicationId,
                applicationNumber = finalApplicationNumber,
                documentCount = documents.Count,
                documents
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            return res.WriteError(HttpStatusCode.BadRequest, "validation_failed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            return res.WriteError(HttpStatusCode.InternalServerError, "server_error", ex.Message);
        }
    }

    // --- utilities ---

    private static async Task CopyWithLimitAsync(Stream src, Stream dst, long maxBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;

        while (true)
        {
            var read = await src.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read <= 0) break;

            total += read;
            if (total > maxBytes)
                throw new InvalidOperationException($"File too large. Max allowed is {maxBytes / (1024 * 1024)} MB.");

            await dst.WriteAsync(buffer, 0, read, ct);
        }
    }

    private static Dictionary<string, string> ParseQuery(Uri url)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var query = url.Query;
        if (string.IsNullOrWhiteSpace(query)) return dict;

        if (query.StartsWith("?")) query = query[1..];
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            var k = Uri.UnescapeDataString(kv[0]);
            var v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
            dict[k] = v;
        }
        return dict;
    }

    private static string FixSqlConnectionString(string cs)
    {
        var b = new SqlConnectionStringBuilder(cs);
        if (b.ConnectTimeout < 60) b.ConnectTimeout = 90;
        if (b.MaxPoolSize < 50) b.MaxPoolSize = 50;
        return b.ConnectionString;
    }

    private static async Task<T> ExecuteWithSqlRetryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        var delays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7) };

        Exception? last = null;
        for (var i = 0; i < delays.Length + 1; i++)
        {
            try { return await action(ct); }
            catch (SqlException ex) { last = ex; }
            catch (InvalidOperationException ex) { last = ex; }

            if (i < delays.Length)
                await Task.Delay(delays[i], ct);
        }

        throw last ?? new Exception("SQL operation failed.");
    }
}