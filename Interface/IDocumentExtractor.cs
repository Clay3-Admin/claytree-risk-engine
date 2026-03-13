using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface;

public interface IDocumentExtractor
{
    Task<DocumentExtractionResult> ExtractAsync(
        LoanDocumentForProcessing document,
        CancellationToken ct);
}