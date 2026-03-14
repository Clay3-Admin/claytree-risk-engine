using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Claytree.Risk.Functions.Functions
{
    public class HealthPingFunction
    {
        [FunctionName("HealthPing")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/ping")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Health ping endpoint called.");

            return new OkObjectResult("OK");
        }
    }
}
