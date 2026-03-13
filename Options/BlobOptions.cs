using System.ComponentModel.DataAnnotations;

namespace Claytree.Risk.Functions.Options;

public sealed class BlobOptions
{
    public const string SectionName = "BLOB";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^[a-z0-9-]{3,63}$", ErrorMessage = "ContainerName must be lowercase, 3-63 chars, letters/numbers/hyphen.")]
    public string ContainerName { get; init; } = "uploads";
}
