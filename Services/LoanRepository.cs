using Claytree.Risk.Functions.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Claytree.Risk.Functions.Services
{
    public sealed class LoanRepository : ILoanRepository
    {
        public async Task<bool> LoanApplicationExistsAsync(SqlConnection cn, SqlTransaction tx, Guid id, CancellationToken ct)
        {
            await using var cmd = new SqlCommand("dbo.sp_LoanApplication_Exists", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@LoanApplicationId", SqlDbType.UniqueIdentifier) { Value = id });

            var v = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(v ?? 0) == 1;
        }

        public async Task<Guid> InsertLoanApplicationAsync(
            SqlConnection cn, SqlTransaction tx,
            string? applicationNumber, string? applicantName, string? mobile, string? email,
            Guid? tenantId, string? createdBy, CancellationToken ct)
        {
            await using var cmd = new SqlCommand("dbo.sp_LoanApplication_Insert", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ApplicationNumber", SqlDbType.NVarChar, 50) { Value = (object?)applicationNumber ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ApplicantName", SqlDbType.NVarChar, 200) { Value = (object?)applicantName ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ApplicantMobile", SqlDbType.NVarChar, 20) { Value = (object?)mobile ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ApplicantEmail", SqlDbType.NVarChar, 200) { Value = (object?)email ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = (object?)tenantId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.NVarChar, 100) { Value = (object?)createdBy ?? DBNull.Value });

            var scalar = await cmd.ExecuteScalarAsync(ct);
            return scalar is Guid g ? g : Guid.Empty;
        }

        //public async Task<Guid> InsertLoanDocumentAsync(
        //    SqlConnection cn, SqlTransaction tx,
        //    Guid loanApplicationId, string documentType, string fileName, string? contentType,
        //    long fileSizeBytes, string blobContainer, string blobPath, string? blobUrl,
        //    string? uploadedBy, CancellationToken ct)
        //{
        //    await using var cmd = new SqlCommand("dbo.sp_LoanDocument_Insert", cn, tx)
        //    {
        //        CommandType = CommandType.StoredProcedure
        //    };

        //    cmd.Parameters.Add(new SqlParameter("@LoanApplicationId", SqlDbType.UniqueIdentifier) { Value = loanApplicationId });
        //    cmd.Parameters.Add(new SqlParameter("@DocumentType", SqlDbType.NVarChar, 100) { Value = documentType });
        //    cmd.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 260) { Value = fileName });
        //    cmd.Parameters.Add(new SqlParameter("@ContentType", SqlDbType.NVarChar, 200) { Value = (object?)contentType ?? DBNull.Value });
        //    cmd.Parameters.Add(new SqlParameter("@FileSizeBytes", SqlDbType.BigInt) { Value = fileSizeBytes });
        //    cmd.Parameters.Add(new SqlParameter("@BlobContainer", SqlDbType.NVarChar, 100) { Value = blobContainer });
        //    cmd.Parameters.Add(new SqlParameter("@BlobPath", SqlDbType.NVarChar, 500) { Value = blobPath });
        //    cmd.Parameters.Add(new SqlParameter("@BlobUrl", SqlDbType.NVarChar, 1000) { Value = (object?)blobUrl ?? DBNull.Value });
        //    cmd.Parameters.Add(new SqlParameter("@UploadedBy", SqlDbType.NVarChar, 100) { Value = (object?)uploadedBy ?? DBNull.Value });

        //    var scalar = await cmd.ExecuteScalarAsync(ct);
        //    return scalar is Guid g ? g : Guid.Empty;
        //}
        public async Task<Guid> InsertLoanDocumentAsync(
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
    // NEW optional metadata
    string? declaredDocumentType = null,
    string? documentOwnerRole = null,
    Guid? entityRefId = null,
    string? documentSide = null,
    string? captureType = null,
    string? languageHint = null,
    string? source = null,
    Guid? replacesLoanDocumentId = null,
    CancellationToken ct = default)
        {
            await using var cmd = new SqlCommand("dbo.sp_LoanDocument_Insert", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@LoanApplicationId", SqlDbType.UniqueIdentifier) { Value = loanApplicationId });
            cmd.Parameters.Add(new SqlParameter("@DocumentType", SqlDbType.NVarChar, 100) { Value = documentType });
            cmd.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 260) { Value = fileName });
            cmd.Parameters.Add(new SqlParameter("@ContentType", SqlDbType.NVarChar, 200) { Value = (object?)contentType ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@FileSizeBytes", SqlDbType.BigInt) { Value = fileSizeBytes });
            cmd.Parameters.Add(new SqlParameter("@BlobContainer", SqlDbType.NVarChar, 100) { Value = blobContainer });
            cmd.Parameters.Add(new SqlParameter("@BlobPath", SqlDbType.NVarChar, 500) { Value = blobPath });
            cmd.Parameters.Add(new SqlParameter("@BlobUrl", SqlDbType.NVarChar, 1000) { Value = (object?)blobUrl ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@UploadedBy", SqlDbType.NVarChar, 100) { Value = (object?)uploadedBy ?? DBNull.Value });

            // NEW params (send DBNull if null)
            cmd.Parameters.Add(new SqlParameter("@DeclaredDocumentType", SqlDbType.NVarChar, 50) { Value = (object?)declaredDocumentType ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@DocumentOwnerRole", SqlDbType.NVarChar, 30) { Value = (object?)documentOwnerRole ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@EntityRefId", SqlDbType.UniqueIdentifier) { Value = (object?)entityRefId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@DocumentSide", SqlDbType.NVarChar, 10) { Value = (object?)documentSide ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@CaptureType", SqlDbType.NVarChar, 20) { Value = (object?)captureType ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@LanguageHint", SqlDbType.NVarChar, 10) { Value = (object?)languageHint ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Source", SqlDbType.NVarChar, 30) { Value = (object?)source ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ReplacesLoanDocumentId", SqlDbType.UniqueIdentifier) { Value = (object?)replacesLoanDocumentId ?? DBNull.Value });

            var scalar = await cmd.ExecuteScalarAsync(ct);
            return scalar is Guid g ? g : Guid.Empty;
        }
        public async Task<string> SetApplicationNumberIfEmptyAsync(
            SqlConnection cn, SqlTransaction tx,
            Guid loanApplicationId, string appNo, CancellationToken ct)
        {
            await using var cmd = new SqlCommand("dbo.sp_LoanApplication_SetAppNoIfEmpty", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@LoanApplicationId", SqlDbType.UniqueIdentifier) { Value = loanApplicationId });
            cmd.Parameters.Add(new SqlParameter("@ApplicationNumber", SqlDbType.NVarChar, 50) { Value = appNo });

            return (await cmd.ExecuteScalarAsync(ct))?.ToString() ?? "";
        }

        public async Task<string> AllocateSerialAsync(
            SqlConnection cn, SqlTransaction tx,
            Guid? tenantId, string branchCode, string productCode, string periodYYMM, int width,
            CancellationToken ct)
        {
            await using var cmd = new SqlCommand("dbo.sp_AppNo_AllocateSerial", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = (object?)tenantId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@BranchCode", SqlDbType.VarChar, 10) { Value = branchCode });
            cmd.Parameters.Add(new SqlParameter("@ProductCode", SqlDbType.VarChar, 10) { Value = productCode });
            cmd.Parameters.Add(new SqlParameter("@PeriodYYMM", SqlDbType.Char, 4) { Value = periodYYMM });
            cmd.Parameters.Add(new SqlParameter("@Width", SqlDbType.Int) { Value = width });

            var outParam = new SqlParameter("@NextPadded", SqlDbType.VarChar, 20)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync(ct);
            return outParam.Value?.ToString() ?? "000000";
        }
    }
}