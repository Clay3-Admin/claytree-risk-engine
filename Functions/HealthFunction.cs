using System.Net;
using Claytree.Risk.Functions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Claytree.Risk.Functions.Functions;

public sealed class HealthFunction
{
    [Function("health")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        => req.CreateResponse().WriteJson(HttpStatusCode.OK, new { status = "ok" });
}
