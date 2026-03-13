namespace Claytree.Risk.Functions.Models;

public sealed class LoanDocumentForProcessing
{
    public Guid LoanDocumentId { get; set; }
    public Guid LoanApplicationId { get; set; }
    public Guid? TenantId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public string BlobContainer { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;

    public int ProcessingStatus { get; set; }
}