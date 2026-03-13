using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Claytree.Risk.Functions.Services;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobOptions _opt;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<BlobOptions> opt, ILogger<BlobStorageService> logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<UploadResult> UploadAsync(Stream content, string originalFileName, string contentType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opt.ConnectionString))
            throw new InvalidOperationException("BLOB__ConnectionString is missing.");

        if (string.IsNullOrWhiteSpace(_opt.ContainerName))
            throw new InvalidOperationException("BLOB__ContainerName is missing.");

        var container = new BlobContainerClient(_opt.ConnectionString, _opt.ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var ext = Path.GetExtension(originalFileName);
        var blobName = $"{Guid.NewGuid():N}{ext}".ToLowerInvariant();
        var blob = container.GetBlobClient(blobName);

        if (content.CanSeek)
            content.Position = 0;

        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };

        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = headers }, ct);

        return new UploadResult
        {
            BlobName = blobName,
            BlobUrl = blob.Uri.ToString(),
            SizeBytes = content.CanSeek ? content.Length : 0,
            ContentType = headers.ContentType!,
            OriginalFileName = originalFileName
        };
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opt.ConnectionString)) return;
        if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName)) return;

        var container = new BlobContainerClient(_opt.ConnectionString, containerName);
        var blob = container.GetBlobClient(blobName);

        try
        {
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob delete failed for {Container}/{Blob}", containerName, blobName);
            throw;
        }
    }
}
