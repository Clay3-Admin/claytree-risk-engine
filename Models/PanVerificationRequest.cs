using System;

namespace Claytree.Risk.Functions.Models
{
    public class PanVerificationRequest
    {
        public string UserId { get; set; }
        public string PanNumber { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool Consent { get; set; }
        public string RequestSource { get; set; }
    }
}
