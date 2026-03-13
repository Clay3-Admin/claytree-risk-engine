using System.ComponentModel.DataAnnotations;

namespace Claytree.Risk.Functions.Options;

public sealed class CosmosOptions
{
    public const string SectionName = "COSMOS";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Database { get; init; } = "infinite-pdf-preview";

    [Required]
    public string Container { get; init; } = "referral";
}
