//namespace Claytree.Risk.Functions.Models;

//public sealed class UploadResult
//{
//    public string BlobName { get; set; } = "";         // e.g. "7a9c...pdf"
//    public string BlobUrl { get; set; } = "";          // blob.Uri
//    public long SizeBytes { get; set; }                // content.Length
//    public string ContentType { get; set; } = "";
//    public string OriginalFileName { get; set; } = "";
//}
namespace Claytree.Risk.Functions.Models;

public sealed class UploadResult
{
    public string BlobName { get; set; } = "";        // the blob path/name inside the container (guid.ext)
    public string BlobUrl { get; set; } = "";         // full blob URL
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
}