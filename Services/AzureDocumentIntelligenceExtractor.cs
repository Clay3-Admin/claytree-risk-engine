using Azure;
using Azure.AI.DocumentIntelligence;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Claytree.Risk.Functions.Services;

public sealed class AzureDocumentIntelligenceExtractor : IDocumentExtractor
{
    private readonly DocumentIntelligenceOptions _options;
    private readonly DocumentIntelligenceClient _client;
    private readonly ILoanDocumentProcessingRepository _repo;

    public AzureDocumentIntelligenceExtractor(
        IOptions<DocumentIntelligenceOptions> options,
        ILoanDocumentProcessingRepository repo)
    {
        _options = options.Value;
        _repo = repo;

        var endpoint = new Uri(_options.Endpoint);
        var credential = new AzureKeyCredential(_options.ApiKey);

        _client = new DocumentIntelligenceClient(endpoint, credential);
    }

    public async Task<DocumentExtractionResult> ExtractAsync(
        LoanDocumentForProcessing document,
        CancellationToken ct)
    {
        try
        {
            // Prefer blob URL if your blobs are reachable by Document Intelligence.
            // For private blobs, switch later to streaming/download flow.
            var sourceUri = new Uri(document.BlobUrl);

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                _options.ModelId,
                sourceUri,
                cancellationToken: ct);

            var result = operation.Value;

            var rawJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var summary = BuildSummary(result);

            return new DocumentExtractionResult
            {
                Success = true,
                ExtractorName = "AzureDocumentIntelligence",
                ExtractorModel = _options.ModelId,
                ExtractionStatus = "Success",
                RawJson = rawJson,
                SummaryJson = JsonSerializer.Serialize(summary),
                Warnings = summary.Warnings
            };
        }
        catch (Exception ex)
        {
            return new DocumentExtractionResult
            {
                Success = false,
                ExtractorName = "AzureDocumentIntelligence",
                ExtractorModel = _options.ModelId,
                ExtractionStatus = "Failed",
                RawJson = string.Empty,
                SummaryJson = JsonSerializer.Serialize(new
                {
                    error = ex.Message
                }),
                Warnings = new List<string>()
            };
        }
    }

    private static ExtractionSummary BuildSummary(AnalyzeResult result)
    {
        var fullText = result.Content ?? string.Empty;

        var pages = result.Pages?.Count ?? 0;
        var tables = result.Tables?.Count ?? 0;
        var paragraphs = result.Paragraphs?.Count ?? 0;

        var lines = result.Pages?.Sum(p => p.Lines?.Count ?? 0) ?? 0;
        var words = result.Pages?.Sum(p => p.Words?.Count ?? 0) ?? 0;

        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(fullText))
            warnings.Add("No text content extracted.");

        return new ExtractionSummary
        {
            PageCount = pages,
            TableCount = tables,
            ParagraphCount = paragraphs,
            LineCount = lines,
            WordCount = words,
            FullText = fullText,
            Warnings = warnings
        };
    }

    private sealed class ExtractionSummary
    {
        public int PageCount { get; set; }
        public int TableCount { get; set; }
        public int ParagraphCount { get; set; }
        public int LineCount { get; set; }
        public int WordCount { get; set; }
        public string FullText { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
    }
}