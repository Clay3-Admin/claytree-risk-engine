using System.Threading.Tasks;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface
{
    public interface IPanVerificationRepository
    {
        Task InsertRequestAsync(string requestId, PanVerificationRequest request, string panHash, string last4);
        Task InsertResultAsync(string requestId, PanProviderResult providerResult, double score);
    }
}
