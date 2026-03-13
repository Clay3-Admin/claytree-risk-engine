```csharp
// file: Models/GstValidationRequest.cs
using System;

namespace ClayTree.Risk.Models
{
    public class GstValidationRequest
    {
        public string GstNumber { get; set; }
        public string EntityName { get; set; }
        public string Pan { get; set; }
        public string RequestSource { get; set; }
        public bool Async { get; set; }
    }
}

// file: Models/GstValidationResponse.cs
using System;
using System.Collections.Generic;

namespace ClayTree.Risk.Models
{
    public class GstValidationResponse
    {
        public string GstNumber { get; set; }
        public bool Valid { get; set; }
        public string Status { get; set; }
        public string EntityName { get; set; }
        public string StateCode { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public List<string> RiskFlags { get; set; } = new();
        public DateTime ValidatedAt { get; set; }
    }
}

// file: Models/GstValidationQueueMessage.cs
using System;

namespace ClayTree.Risk.Models
{
    public class GstValidationQueueMessage
    {
        public Guid ValidationId { get; set; }
        public string GstNumber { get; set; }
        public string Pan { get; set; }
        public string EntityName { get; set; }
    }
}

// file: Interfaces/IGstValidationService.cs
using System;
using System.Threading.Tasks;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Interfaces
{
    public interface IGstValidationService
    {
        Task<GstValidationResponse> ValidateAsync(GstValidationRequest request);
        Task<Guid> QueueValidationAsync(GstValidationRequest request);
        Task<GstValidationResponse> GetValidationResultAsync(Guid validationId);
    }
}

// file: Interfaces/IGstRepository.cs
using System;
using System.Threading.Tasks;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Interfaces
{
    public interface IGstRepository
    {
        Task SaveRequestAsync(Guid id, GstValidationRequest request, string status);
        Task UpdateRequestStatusAsync(Guid id, string status);
        Task SaveResultAsync(Guid id, GstValidationResponse response);
        Task<GstValidationResponse> GetResultAsync(Guid id);
    }
}

// file: Interfaces/IGstProvider.cs
using System.Threading.Tasks;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Interfaces
{
    public interface IGstProvider
    {
        Task<GstValidationResponse> FetchAsync(string gstNumber);
    }
}

// file: Services/GstValidationService.cs
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClayTree.Risk.Interfaces;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Services
{
    public class GstValidationService : IGstValidationService
    {
        private readonly IGstRepository _repository;
        private readonly IGstProvider _provider;

        private const string GstPattern = "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$";

        public GstValidationService(IGstRepository repository, IGstProvider provider)
        {
            _repository = repository;
            _provider = provider;
        }

        public async Task<GstValidationResponse> ValidateAsync(GstValidationRequest request)
        {
            if (!Regex.IsMatch(request.GstNumber, GstPattern))
            {
                return new GstValidationResponse
                {
                    GstNumber = request.GstNumber,
                    Valid = false,
                    Status = "INVALID_FORMAT",
                    ValidatedAt = DateTime.UtcNow
                };
            }

            var providerResult = await _provider.FetchAsync(request.GstNumber);

            providerResult.ValidatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Pan))
            {
                var panFromGst = request.GstNumber.Substring(2, 10);
                if (!panFromGst.Equals(request.Pan, StringComparison.OrdinalIgnoreCase))
                {
                    providerResult.RiskFlags.Add("PAN_MISMATCH");
                }
            }

            if (providerResult.RegistrationDate.HasValue)
            {
                if ((DateTime.UtcNow - providerResult.RegistrationDate.Value).TotalDays < 180)
                {
                    providerResult.RiskFlags.Add("NEW_REGISTRATION");
                }
            }

            if (providerResult.Status != null &&
                (providerResult.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                 providerResult.Status.Equals("SUSPENDED", StringComparison.OrdinalIgnoreCase)))
            {
                providerResult.RiskFlags.Add("INACTIVE_GST");
            }

            return providerResult;
        }

        public async Task<Guid> QueueValidationAsync(GstValidationRequest request)
        {
            var id = Guid.NewGuid();
            await _repository.SaveRequestAsync(id, request, "QUEUED");
            return id;
        }

        public Task<GstValidationResponse> GetValidationResultAsync(Guid validationId)
        {
            return _repository.GetResultAsync(validationId);
        }
    }
}

// file: Infrastructure/GstRepository.cs
using System;
using System.Threading.Tasks;
using ClayTree.Risk.Interfaces;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Infrastructure
{
    public class GstRepository : IGstRepository
    {
        public Task SaveRequestAsync(Guid id, GstValidationRequest request, string status)
        {
            return Task.CompletedTask;
        }

        public Task UpdateRequestStatusAsync(Guid id, string status)
        {
            return Task.CompletedTask;
        }

        public Task SaveResultAsync(Guid id, GstValidationResponse response)
        {
            return Task.CompletedTask;
        }

        public Task<GstValidationResponse> GetResultAsync(Guid id)
        {
            return Task.FromResult<GstValidationResponse>(null);
        }
    }
}

// file: Infrastructure/MockGstProvider.cs
using System;
using System.Threading.Tasks;
using ClayTree.Risk.Interfaces;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Infrastructure
{
    public class MockGstProvider : IGstProvider
    {
        public Task<GstValidationResponse> FetchAsync(string gstNumber)
        {
            var response = new GstValidationResponse
            {
                GstNumber = gstNumber,
                Valid = true,
                Status = "ACTIVE",
                EntityName = "Sample Entity",
                StateCode = gstNumber.Substring(0, 2),
                RegistrationDate = new DateTime(2018, 4, 1),
                ValidatedAt = DateTime.UtcNow
            };

            return Task.FromResult(response);
        }
    }
}

// file: Functions/GstValidationIngestFunction.cs
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using ClayTree.Risk.Interfaces;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Functions
{
    public class GstValidationIngestFunction
    {
        private readonly IGstValidationService _service;

        public GstValidationIngestFunction(IGstValidationService service)
        {
            _service = service;
        }

        [Function("gst-validation-ingest")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/risk/gst/validate")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<GstValidationRequest>(body);

            var result = await _service.ValidateAsync(request);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            return response;
        }
    }
}

// file: Functions/GstValidationWorkerFunction.cs
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using ClayTree.Risk.Interfaces;
using ClayTree.Risk.Models;

namespace ClayTree.Risk.Functions
{
    public class GstValidationWorkerFunction
    {
        private readonly IGstValidationService _service;
        private readonly IGstRepository _repository;

        public GstValidationWorkerFunction(IGstValidationService service, IGstRepository repository)
        {
            _service = service;
            _repository = repository;
        }

        [Function("gst-validation-worker")]
        public async Task Run([QueueTrigger("gst-validation-queue")] string message)
        {
            var payload = JsonSerializer.Deserialize<GstValidationQueueMessage>(message);

            await _repository.UpdateRequestStatusAsync(payload.ValidationId, "PROCESSING");

            var request = new GstValidationRequest
            {
                GstNumber = payload.GstNumber,
                Pan = payload.Pan,
                EntityName = payload.EntityName
            };

            var result = await _service.ValidateAsync(request);

            await _repository.SaveResultAsync(payload.ValidationId, result);
            await _repository.UpdateRequestStatusAsync(payload.ValidationId, "COMPLETED");
        }
    }
}

// file: Functions/GstCacheRefreshFunction.cs
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

namespace ClayTree.Risk.Functions
{
    public class GstCacheRefreshFunction
    {
        [Function("gst-cache-refresh")]
        public Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo timer)
        {
            return Task.CompletedTask;
        }
    }
}
```