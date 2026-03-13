using System.ComponentModel.DataAnnotations;

namespace Claytree.Risk.Functions.Options;

public sealed class UploadOptions
{
    public const string SectionName = "UPLOAD";

    [Range(1, 200)]
    public int MaxFileMb { get; init; } = 25;

    public string[] AllowedExtensions { get; init; } = [".pdf", ".tiff", ".tif", ".jpeg", ".jpg", ".png"];
}
