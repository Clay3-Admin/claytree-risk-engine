using System.Net;
using System.Text.Json;
using Claytree.Risk.Functions.Extensions;
using Claytree.Risk.Functions.Interface;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Claytree.Risk.Functions.Functions;

public sealed class UpdateReferralFunction
{
    private readonly ILogger<UpdateReferralFunction> _logger;
    private readonly ICosmosReferralRepository _repo;

    public UpdateReferralFunction(ILogger<UpdateReferralFunction> logger, ICosmosReferralRepository repo)
    {
        _logger = logger;
        _repo = repo;
    }

    [Function("update-referral")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "referrals/{id}")] HttpRequestData req, string id, FunctionContext ctx)
    {
        var res = req.CreateResponse();

        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return res.WriteError(HttpStatusCode.BadRequest, "missing_id", "id is required in route.");

            using var sr = new StreamReader(req.Body);
            var body = await sr.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return res.WriteError(HttpStatusCode.BadRequest, "empty_body", "Request body is required.");

            var updates = JsonSerializer.Deserialize<Dictionary<string, object>>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (updates is null || updates.Count == 0)
                return res.WriteError(HttpStatusCode.BadRequest, "invalid_body", "Body must be a JSON object with fields to update.");

            var updated = await _repo.PatchAsync(id, updates, ctx.CancellationToken);

            return res.WriteJson(HttpStatusCode.OK, new { message = "Referral updated successfully", id, updated });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Update failed");
            return res.WriteError(HttpStatusCode.NotFound, "not_found", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update error");
            return res.WriteError(HttpStatusCode.InternalServerError, "server_error", ex.Message);
        }
    }
}
