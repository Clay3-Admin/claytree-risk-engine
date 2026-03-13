using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Services;

public sealed class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly ILoanDocumentFingerprintRepository _repo;

    public DuplicateDetectionService(ILoanDocumentFingerprintRepository repo)
    {
        _repo = repo;
    }

    public async Task<DuplicateDetectionResult> CheckAsync(
        FileFingerprintResult fingerprint,
        Guid? tenantId,
        CancellationToken ct)
    {
        var match = await _repo.FindBySha256Async(fingerprint.Sha256Hash, tenantId, ct);

        if (match.Found)
        {
            return new DuplicateDetectionResult
            {
                IsDuplicate = true,
                IsNearDuplicate = false,
                DuplicateType = "Exact",
                MatchedLoanDocumentId = match.LoanDocumentId,
                MatchedLoanApplicationId = match.LoanApplicationId,
                MatchReason = "Exact SHA256 hash match found."
            };
        }

        return new DuplicateDetectionResult
        {
            IsDuplicate = false,
            IsNearDuplicate = false,
            DuplicateType = "None"
        };
    }
}