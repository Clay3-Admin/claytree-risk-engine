using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Interface;

namespace Claytree.Risk.Functions.Functions
{
    public class PanVerificationFunction
    {
        private readonly IPanVerificationService _service;
        private readonly ILogger<PanVerificationFunction> _logger;

        public PanVerificationFunction(IPanVerificationService service, ILogger<PanVerificationFunction> logger)
        {
            _service = service;
            _logger = logger;
        }

        [Function("verifyPanHttpTrigger")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "risk/pan/verify")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<PanVerificationRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var result = await _service.VerifyAsync(request);

            var response = req.CreateResponse((System.Net.HttpStatusCode)result.StatusCode);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
