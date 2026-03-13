using Claytree.Risk.Functions.Extensions;
using Claytree.Risk.Functions.Infrastructure;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Options;
using Claytree.Risk.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Claytree.Risk.Functions.Functions;

public sealed class ValidateFileStructureFunction
{
    private readonly ILogger<ValidateFileStructureFunction> _logger;
    private readonly FileValidationOptions _validationOptions;
    private readonly IFileStructuralValidator _validator;

    public ValidateFileStructureFunction(
        ILogger<ValidateFileStructureFunction> logger,
        IOptions<FileValidationOptions> validationOptions,
        IFileStructuralValidator validator)
    {
        _logger = logger;
        _validationOptions = validationOptions.Value;
        _validator = validator;
    }

    [Function("validate-file-structure")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "validate-file-structure")] HttpRequestData req,
        FunctionContext ctx)
    {
        var res = req.CreateResponse();
        var ct = ctx.CancellationToken;

        try
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var values)
                ? values.FirstOrDefault() ?? ""
                : "";

            if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                return res.WriteError(HttpStatusCode.BadRequest, "invalid_content_type", "Expected multipart/form-data.");

            var fileParts = await MultipartFormDataReader.ReadAllFilesAsync(req.Body, contentType, ct);
            if (fileParts.Count == 0)
                return res.WriteError(HttpStatusCode.BadRequest, "no_file", "No file found in multipart request.");

            var results = new List<FileValidationResult>();

            foreach (var fp in fileParts)
            {
                using var ms = new MemoryStream();

                if (fp.Content.CanSeek)
                    fp.Content.Position = 0;

                await CopyWithLimitAsync(
                    fp.Content,
                    ms,
                    (_validationOptions.MaxFileSizeMb * 1024L * 1024L) + 1,
                    ct);

                ms.Position = 0;

                var result = await _validator.ValidateAsync(
                    fp.FileName,
                    fp.ContentType,
                    ms,
                    ct);

                results.Add(result);
            }

            var allValid = results.All(x => x.IsValid);

            return res.WriteJson(allValid ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity, new
            {
                message = allValid
                    ? "All files passed structural validation."
                    : "One or more files failed structural validation.",
                fileCount = results.Count,
                files = results
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            return res.WriteError(HttpStatusCode.BadRequest, "validation_failed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validate file structure failed");
            return res.WriteError(HttpStatusCode.InternalServerError, "server_error", ex.Message);
        }
    }

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
}