using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Claytree.Risk.Functions.Interface
{
    public interface ILoanRepository
    {
        Task<bool> LoanApplicationExistsAsync(SqlConnection cn, SqlTransaction tx, Guid id, CancellationToken ct);

        Task<Guid> InsertLoanApplicationAsync(SqlConnection cn, SqlTransaction tx,
            string? applicationNumber, string? applicantName, string? mobile, string? email,
            Guid? tenantId, string? createdBy, CancellationToken ct);

        Task<Guid> InsertLoanDocumentAsync(
    SqlConnection cn, SqlTransaction tx,
    Guid loanApplicationId,
    string documentType,
    string fileName,
    string? contentType,
    long fileSizeBytes,
    string blobContainer,
    string blobPath,
    string? blobUrl,
    string? uploadedBy,
    string? declaredDocumentType = null,
    string? documentOwnerRole = null,
    Guid? entityRefId = null,
    string? documentSide = null,
    string? captureType = null,
    string? languageHint = null,
    string? source = null,
    Guid? replacesLoanDocumentId = null,
    CancellationToken ct = default);


        Task<string> SetApplicationNumberIfEmptyAsync(SqlConnection cn, SqlTransaction tx, Guid loanApplicationId, string appNo, CancellationToken ct);

        Task<string> AllocateSerialAsync(SqlConnection cn, SqlTransaction tx, Guid? tenantId, string branchCode, string productCode, string periodYYMM, int width, CancellationToken ct);
    }
}
 
