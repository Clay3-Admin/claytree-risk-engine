using System.ComponentModel.DataAnnotations;

namespace Claytree.Risk.Functions.Options;

public sealed class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    // Good default starter model
    public string ModelId { get; set; } = "prebuilt-layout";

    // Optional future use
    public bool UseBlobUrlDirect { get; set; } = true;
}