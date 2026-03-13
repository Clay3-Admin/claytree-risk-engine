namespace Claytree.Risk.Functions.Models;

public sealed class PreparedUploadFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public MemoryStream Content { get; set; } = new();
    public FileValidationResult Validation { get; set; } = new();
    public FileSecurityValidationResult SecurityValidation { get; set; } = new();
    public FileFingerprintResult Fingerprint { get; set; } = new();
    public DuplicateDetectionResult DuplicateDetection { get; set; } = new();
}