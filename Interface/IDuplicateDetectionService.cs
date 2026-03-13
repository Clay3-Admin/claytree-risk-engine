using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface IDuplicateDetectionService
{
    Task<DuplicateDetectionResult> CheckAsync(
        FileFingerprintResult fingerprint,
        Guid? tenantId,
        CancellationToken ct);
}