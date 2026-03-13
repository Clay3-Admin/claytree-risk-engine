using System;
using System.Threading.Tasks;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Infrastructure
{
    public class KarzaPanProvider : IPanProvider
    {
        public Task<PanProviderResult> VerifyPanAsync(string panNumber, string name, DateTime dateOfBirth)
        {
            var result = new PanProviderResult
            {
                Success = true,
                PanStatus = "ACTIVE",
                Category = "INDIVIDUAL",
                NameOnPan = name,
                DobOnPan = dateOfBirth,
                RawResponse = "{}"
            };

            return Task.FromResult(result);
        }
    }
}
