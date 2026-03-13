using System;
using System.Threading.Tasks;
using Claytree.Risk.Functions.Models;

namespace Claytree.Risk.Functions.Interface
{
    public interface IPanProvider
    {
        Task<PanProviderResult> VerifyPanAsync(string panNumber, string name, DateTime dateOfBirth);
    }
}
