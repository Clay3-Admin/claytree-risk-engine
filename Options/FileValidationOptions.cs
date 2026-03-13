namespace Claytree.Risk.Functions.Options;

public sealed class FileValidationOptions
{
    public int MaxFileSizeMb { get; set; } = 500;
    public int MaxPdfPages { get; set; } = 2000;
    public int MinWidthPx { get; set; } = 50;
    public int MinHeightPx { get; set; } = 50;
    public int MaxWidthPx { get; set; } = 10000;
    public int MaxHeightPx { get; set; } = 10000;
    public int MinDpi { get; set; } = 72;
    public int MaxDpi { get; set; } = 1200;

    public HashSet<string> AllowedExtensions { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp",
            ".tif",
            ".tiff",
            ".webp"
        };
}