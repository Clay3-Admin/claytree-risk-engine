using System;

namespace Claytree.Risk.Functions.Models
{
    public class PanVerificationResponse
    {
        public string RequestId { get; set; }
        public string PanNumber { get; set; }
        public string Status { get; set; }
        public double NameMatchScore { get; set; }
        public string PanStatus { get; set; }
        public string Category { get; set; }
        public DateTime VerifiedAt { get; set; }
        public int StatusCode { get; set; }
        public string Reason { get; set; }

        public static PanVerificationResponse Fail(string reason, int code, string requestId = null)
        {
            return new PanVerificationResponse
            {
                RequestId = requestId,
                Status = "FAILED",
                Reason = reason,
                StatusCode = code
            };
        }
    }
}
