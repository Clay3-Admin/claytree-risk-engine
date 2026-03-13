using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Claytree.Risk.Functions.Services;

public sealed class CosmosReferralRepository : ICosmosReferralRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosReferralRepository> _logger;

    public CosmosReferralRepository(IOptions<CosmosOptions> opt, ILogger<CosmosReferralRepository> logger)
    {
        _logger = logger;
        var o = opt.Value;
        var client = new CosmosClient(o.Endpoint, o.Key);
        _container = client.GetContainer(o.Database, o.Container);
    }

    public async Task<IDictionary<string, object>> PatchAsync(string id, IDictionary<string, object> updates, CancellationToken ct)
    {
        try
        {
            var read = await _container.ReadItemAsync<IDictionary<string, object>>(id, new PartitionKey(id), cancellationToken: ct);
            var doc = read.Resource;

            foreach (var kv in updates)
                doc[kv.Key] = kv.Value;

            await _container.ReplaceItemAsync(doc, id, new PartitionKey(id), cancellationToken: ct);
            _logger.LogInformation("Updated cosmos doc id={Id} fields={Count}", id, updates.Count);
            return doc;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Referral '{id}' not found.");
        }
    }
}
