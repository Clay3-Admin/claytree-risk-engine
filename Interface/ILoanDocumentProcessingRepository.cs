using System;
using System.Threading;
using System.Threading.Tasks;

namespace Claytree.Risk.Functions.Interface;

public interface ILoanDocumentProcessingRepository
{
    Task UpdateProcessingStatusAsync(
        Guid loanDocumentId,
        int processingStatus,
        DateTime? startedAt,
        DateTime? completedAt,
        string? errorCode,
        string? errorMessage,
        CancellationToken ct);
}