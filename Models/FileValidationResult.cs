namespace Claytree.Risk.Functions.Models;

public sealed class FileValidationResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public bool IsValid { get; set; }
    public int? PageCount { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public double? HorizontalDpi { get; set; }
    public double? VerticalDpi { get; set; }
    public List<string> Errors { get; set; } = new();

    public List<string> Warnings { get; set; } = new();   // NEW
}