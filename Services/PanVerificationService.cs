using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Services
{
    public class PanVerificationService : IPanVerificationService
    {
        private readonly IPanProvider _provider;
        private readonly IPanVerificationRepository _repo;
        private readonly INameMatchService _nameMatch;

        public PanVerificationService(IPanProvider provider, IPanVerificationRepository repo, INameMatchService nameMatch)
        {
            _provider = provider;
            _repo = repo;
            _nameMatch = nameMatch;
        }

        public async Task<PanVerificationResponse> VerifyAsync(PanVerificationRequest request)
        {
            if (request == null || !request.Consent)
                return PanVerificationResponse.Fail("CONSENT_REQUIRED", 400);

            var pan = request.PanNumber?.Trim().ToUpper();

            if (!Regex.IsMatch(pan ?? "", "^[A-Z]{5}[0-9]{4}[A-Z]$"))
                return PanVerificationResponse.Fail("INVALID_PAN", 400);

            var requestId = "panreq_" + Guid.NewGuid().ToString("N");

            var panHash = HashPan(pan);
            var last4 = pan.Substring(pan.Length - 4);

            await _repo.InsertRequestAsync(requestId, request, panHash, last4);

            var providerResult = await _provider.VerifyPanAsync(pan, request.Name, request.DateOfBirth);

            if (!providerResult.Success)
                return PanVerificationResponse.Fail("PROVIDER_ERROR", 500, requestId);

            var score = _nameMatch.ComputeScore(request.Name, providerResult.NameOnPan);

            await _repo.InsertResultAsync(requestId, providerResult, score);

            return new PanVerificationResponse
            {
                RequestId = requestId,
                PanNumber = pan,
                Status = "VERIFIED",
                NameMatchScore = score,
                PanStatus = providerResult.PanStatus,
                Category = providerResult.Category,
                VerifiedAt = DateTime.UtcNow,
                StatusCode = 200
            };
        }

        private string HashPan(string pan)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pan));
            return Convert.ToHexString(bytes);
        }
    }
}
