using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Claytree.Risk.Functions.Interface
{
    public interface IApplicationNumberService
    {
        Task<string> EnsureAsync(SqlConnection cn, SqlTransaction tx,
            Guid loanApplicationId, Guid? tenantId,
            string? branchCode, string? productCode,
            string? requestedApplicationNumber,
            CancellationToken ct);
    }
}
