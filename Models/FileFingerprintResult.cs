namespace Claytree.Risk.Functions.Models;

public sealed class FileFingerprintResult
{
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;

    public string Sha256Hash { get; set; } = string.Empty;          // exact duplicate
    public string? ContentNormalizedHash { get; set; }              // metadata-insensitive for PDF/text-like normalization
    public string? PerceptualHash { get; set; }                     // near-duplicate image detection
    public long FileSizeBytes { get; set; }

    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public int? PageCount { get; set; }

    public List<string> Warnings { get; set; } = new();
}