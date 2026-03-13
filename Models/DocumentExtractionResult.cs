namespace Claytree.Risk.Functions.Models;

public sealed class DocumentExtractionResult
{
    public bool Success { get; set; }
    public string ExtractorName { get; set; } = "AzureDocumentIntelligence";
    public string? ExtractorModel { get; set; }
    public string ExtractionStatus { get; set; } = string.Empty;

    public string RawJson { get; set; } = string.Empty;
    public string SummaryJson { get; set; } = string.Empty;

    public List<string> Warnings { get; set; } = new();
}