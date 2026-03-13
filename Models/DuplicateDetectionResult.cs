namespace Claytree.Risk.Functions.Models;

public sealed class DuplicateDetectionResult
{
    public bool IsDuplicate { get; set; }
    public bool IsNearDuplicate { get; set; }
    public string DuplicateType { get; set; } = string.Empty; // Exact / Near / CrossApplication / CrossBorrower

    public string? MatchedLoanDocumentId { get; set; }
    public string? MatchedLoanApplicationId { get; set; }
    public string? MatchReason { get; set; }

    public List<string> Warnings { get; set; } = new();
}