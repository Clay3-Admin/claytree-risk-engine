namespace Claytree.Risk.Functions.Models;

public sealed class FileSecurityValidationResult
{
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string DeclaredContentType { get; set; } = string.Empty;

    public bool IsSafe { get; set; } = true;

    public string? DetectedFileType { get; set; }
    public bool SignatureMatched { get; set; }

    public bool IsEncrypted { get; set; }
    public bool HasMacros { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}