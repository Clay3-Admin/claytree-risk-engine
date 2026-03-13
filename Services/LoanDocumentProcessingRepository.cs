using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Claytree.Risk.Functions.Services;

public sealed class LoanDocumentProcessingRepository : ILoanDocumentProcessingRepository
{
    private readonly SqlOptions _sqlOptions;

    public LoanDocumentProcessingRepository(IOptions<SqlOptions> sqlOptions)
    {
        _sqlOptions = sqlOptions.Value;
    }

    public async Task UpdateProcessingStatusAsync(
        Guid loanDocumentId,
        int processingStatus,
        DateTime? startedAt,
        DateTime? completedAt,
        string? errorCode,
        string? errorMessage,
        CancellationToken ct)
    {
        await using var cn = new SqlConnection(_sqlOptions.ConnectionString);
        await cn.OpenAsync(ct);

        await using var cmd = new SqlCommand("dbo.sp_LoanDocument_UpdateProcessingStatus", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@LoanDocumentId", SqlDbType.UniqueIdentifier).Value = loanDocumentId;
        cmd.Parameters.Add("@ProcessingStatus", SqlDbType.Int).Value = processingStatus;
        cmd.Parameters.Add("@ProcessingStartedAt", SqlDbType.DateTime2).Value = (object?)startedAt ?? DBNull.Value;
        cmd.Parameters.Add("@ProcessingCompletedAt", SqlDbType.DateTime2).Value = (object?)completedAt ?? DBNull.Value;
        cmd.Parameters.Add("@ErrorCode", SqlDbType.NVarChar, 100).Value = (object?)errorCode ?? DBNull.Value;
        cmd.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, 2000).Value = (object?)errorMessage ?? DBNull.Value;

        await cmd.ExecuteNonQueryAsync(ct);
    }
}