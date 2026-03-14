using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Claytree.Risk.Functions.Functions
{
    public class HealthPingFunction
    {
        [Function("HealthPing")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("OK");
            return response;
        }
    }
}
