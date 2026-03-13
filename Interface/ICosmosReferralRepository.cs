namespace Claytree.Risk.Functions.Interface;

public interface ICosmosReferralRepository
{
    Task<IDictionary<string, object>> PatchAsync(string id, IDictionary<string, object> updates, CancellationToken ct);
}
