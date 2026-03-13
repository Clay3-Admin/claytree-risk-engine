using System.Threading.Tasks;
using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Infrastructure
{
    public class PanVerificationRepository : IPanVerificationRepository
    {
        public Task InsertRequestAsync(string requestId, PanVerificationRequest request, string panHash, string last4)
        {
            return Task.CompletedTask;
        }

        public Task InsertResultAsync(string requestId, PanProviderResult providerResult, double score)
        {
            return Task.CompletedTask;
        }
    }
}
