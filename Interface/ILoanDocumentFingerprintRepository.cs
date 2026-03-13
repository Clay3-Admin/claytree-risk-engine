using Claytree.Risk.Functions.Models;
using Microsoft.Data.SqlClient;

namespace Claytree.Risk.Functions.Interface;

public interface ILoanDocumentFingerprintRepository
{
    Task<(bool Found, string? LoanDocumentId, string? LoanApplicationId)> FindBySha256Async(
        string sha256Hash,
        Guid? tenantId,
        CancellationToken ct);

    Task SaveFingerprintAsync(
        SqlConnection cn,
        SqlTransaction tx,
        Guid loanDocumentId,
        Guid loanApplicationId,
        Guid? tenantId,
        FileFingerprintResult fingerprint,
        CancellationToken ct);
}