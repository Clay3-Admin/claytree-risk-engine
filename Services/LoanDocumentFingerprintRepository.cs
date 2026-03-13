using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Models;
using Claytree.Risk.Functions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace Claytree.Risk.Functions.Services;

public sealed class LoanDocumentFingerprintRepository : ILoanDocumentFingerprintRepository
{
    private readonly SqlOptions _sqlOptions;

    public LoanDocumentFingerprintRepository(IOptions<SqlOptions> sqlOptions)
    {
        _sqlOptions = sqlOptions.Value;
    }

    public async Task<(bool Found, string? LoanDocumentId, string? LoanApplicationId)> FindBySha256Async(
        string sha256Hash,
        Guid? tenantId,
        CancellationToken ct)
    {
        const int maxAttempts = 3;
        var attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                await using var cn = new SqlConnection(SqlHelper.FixSqlConnectionString(_sqlOptions.ConnectionString));
                await cn.OpenAsync(ct);

                await using var cmd = new SqlCommand("dbo.sp_LoanDocumentFingerprint_FindBySha256", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@Sha256Hash", SqlDbType.VarChar, 64).Value = sha256Hash;
                cmd.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = (object?)tenantId ?? DBNull.Value;

                await using var rdr = await cmd.ExecuteReaderAsync(ct);

                if (await rdr.ReadAsync(ct))
                {
                    return (
                        true,
                        rdr.IsDBNull(0) ? null : rdr.GetString(0),
                        rdr.IsDBNull(1) ? null : rdr.GetString(1)
                    );
                }

                return (false, null, null);
            }
            catch (SqlException ex) when (IsTransientSqlException(ex) && attempt < maxAttempts)
            {
                var delayMs = 250 * attempt;
                await Task.Delay(delayMs, ct);
                continue;
            }
            catch (Exception ex) when (IsTransientWin32Timeout(ex) && attempt < maxAttempts)
            {
                var delayMs = 250 * attempt;
                await Task.Delay(delayMs, ct);
                continue;
            }
        }
    }

    private static bool IsTransientSqlException(SqlException ex)
    {
        // -2 is timeout, other transient numbers could be added as needed
        return ex.Number == -2;
    }

    private static bool IsTransientWin32Timeout(Exception ex)
    {
        if (ex is System.ComponentModel.Win32Exception w32)
        {
            return w32.NativeErrorCode == 258;
        }

        if (ex.InnerException is System.ComponentModel.Win32Exception inner)
        {
            return inner.NativeErrorCode == 258;
        }

        return false;
    }

    public async Task SaveFingerprintAsync(
        SqlConnection cn,
        SqlTransaction tx,
        Guid loanDocumentId,
        Guid loanApplicationId,
        Guid? tenantId,
        FileFingerprintResult fingerprint,
        CancellationToken ct)
    {
        await using var cmd = new SqlCommand("dbo.sp_LoanDocumentFingerprint_Save", cn, tx)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@LoanDocumentId", SqlDbType.UniqueIdentifier).Value = loanDocumentId;
        cmd.Parameters.Add("@LoanApplicationId", SqlDbType.UniqueIdentifier).Value = loanApplicationId;
        cmd.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = (object?)tenantId ?? DBNull.Value;
        cmd.Parameters.Add("@Sha256Hash", SqlDbType.VarChar, 64).Value = fingerprint.Sha256Hash;
        cmd.Parameters.Add("@ContentNormalizedHash", SqlDbType.VarChar, 64).Value = (object?)fingerprint.ContentNormalizedHash ?? DBNull.Value;
        cmd.Parameters.Add("@PerceptualHash", SqlDbType.VarChar, 64).Value = (object?)fingerprint.PerceptualHash ?? DBNull.Value;
        cmd.Parameters.Add("@FileSizeBytes", SqlDbType.BigInt).Value = fingerprint.FileSizeBytes;
        cmd.Parameters.Add("@WidthPx", SqlDbType.Int).Value = (object?)fingerprint.WidthPx ?? DBNull.Value;
        cmd.Parameters.Add("@HeightPx", SqlDbType.Int).Value = (object?)fingerprint.HeightPx ?? DBNull.Value;
        cmd.Parameters.Add("@PageCount", SqlDbType.Int).Value = (object?)fingerprint.PageCount ?? DBNull.Value;

        await cmd.ExecuteNonQueryAsync(ct);
    }
}