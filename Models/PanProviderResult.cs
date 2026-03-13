using System;

namespace Claytree.Risk.Functions.Models
{
    public class PanProviderResult
    {
        public bool Success { get; set; }
        public string PanStatus { get; set; }
        public string Category { get; set; }
        public string NameOnPan { get; set; }
        public DateTime DobOnPan { get; set; }
        public string RawResponse { get; set; }
    }
}
