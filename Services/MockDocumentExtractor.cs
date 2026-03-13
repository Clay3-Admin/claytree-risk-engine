using Claytree.Risk.Functions.Interface;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Claytree.Risk.Functions.Services;

public sealed class MockDocumentExtractor : IDocumentExtractor
{
    public async Task<string> ExtractAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        await Task.Delay(500, ct);

        var result = new
        {
            fileName,
            contentType,
            extractedAt = System.DateTime.UtcNow,
            message = "Mock extraction successful"
        };

        return JsonSerializer.Serialize(result);
    }
}