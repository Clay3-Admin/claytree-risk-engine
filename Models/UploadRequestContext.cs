namespace Claytree.Risk.Functions.Models;

public sealed class UploadRequestContext
{
    public string DocumentType { get; set; } = string.Empty;
    public string? ApplicationNumber { get; set; }
    public string? ApplicantName { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? CreatedBy { get; set; }
    public string? BranchCode { get; set; }
    public string? ProductCode { get; set; }
    public string? DeclaredDocumentType { get; set; }
    public string? DocumentOwnerRole { get; set; }
    public string? DocumentSide { get; set; }
    public string? CaptureType { get; set; }
    public string? LanguageHint { get; set; }
    public string? Source { get; set; }
    public Guid? ReplacesLoanDocumentId { get; set; }
    public Guid LoanApplicationId { get; set; }
    public Guid? TenantId { get; set; }
}
