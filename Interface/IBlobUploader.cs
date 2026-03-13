using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface IBlobUploader
{
    Task<UploadResult> UploadAsync(Stream content, string originalFileName, string contentType, CancellationToken ct);
    Task DeleteAsync(string containerName, string blobName, CancellationToken ct);
}