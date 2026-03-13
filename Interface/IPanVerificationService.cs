using System.Threading.Tasks;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface
{
    public interface IPanVerificationService
    {
        Task<PanVerificationResponse> VerifyAsync(PanVerificationRequest request);
    }
}
