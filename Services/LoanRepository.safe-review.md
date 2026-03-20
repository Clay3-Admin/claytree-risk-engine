# ClayTree C# Safe Patch Review Report

## Issue
SQL connection timeout during upload flow

## Risk
- Risk Level: High
- Reason: Issue maps to SQL/repository/configuration flow.

## Likely Functions
- upload-file :: `D:\NBFC\src\Claytree.Risk.Functions\Functions\UploadFileFunction - Copy.cs`
- upload-file :: `D:\NBFC\src\Claytree.Risk.Functions\Functions\UploadFileFunction.cs`
- validate-file-structure :: `D:\NBFC\src\Claytree.Risk.Functions\Functions\ValidateFileStructureFunction.cs`

## Top Root Cause Candidates
- `D:\NBFC\src\Claytree.Risk.Functions\Services\LoanDocumentRepository.cs`
  - Role: service_or_repository
  - Score: 85
  - Summary: service_or_repository likely depends on SQL configuration and connection string binding.
- `D:\NBFC\src\Claytree.Risk.Functions\Services\LoanRepository.cs`
  - Role: service_or_repository
  - Score: 85
  - Summary: service_or_repository likely participates in SQL timeout behavior and may lack explicit command timeout handling.
- `D:\NBFC\src\Claytree.Risk.Functions\Services\UploadService.cs`
  - Role: service_or_repository
  - Score: 80
  - Summary: service_or_repository is relevant and should be reviewed for SQL timeout behavior.

## Safe Patch Candidates
- `D:\NBFC\src\Claytree.Risk.Functions\Services\LoanDocumentRepository.cs`
  - Safety: safe_reviewable_patch
  - Proposed Change: Add validation/guard for SQL connection string resolution before opening connection.
  - Proposed Change: Review and standardize SqlConnection creation/disposal pattern with await using where applicable.
- `D:\NBFC\src\Claytree.Risk.Functions\Services\LoanRepository.cs`
  - Safety: safe_reviewable_patch
  - Proposed Change: Add explicit SqlCommand.CommandTimeout assignment near command construction.
  - Proposed Change: Add targeted try/catch around DB open/execute flow with structured logging.
- `D:\NBFC\src\Claytree.Risk.Functions\Services\UploadService.cs`
  - Safety: safe_reviewable_patch
  - Proposed Change: Add targeted try/catch around DB open/execute flow with structured logging.

## Applied Safe Edit
- Status: patched
- File: `D:\NBFC\src\Claytree.Risk.Functions\Services\LoanRepository.cs`
- Output: `D:\NBFC\src\Claytree.Risk.Functions\Services\LoanRepository.safe-review.patched.cs`
- Edit Type: command_timeout
- Reason: Inserted explicit CommandTimeout after SqlCommand initialization.

## Before Snippet
```csharp
ng System.Data;

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

            var v = await cmd.ExecuteScalarA
```

## After Snippet
```csharp
> LoanApplicationExistsAsync(SqlConnection cn, SqlTransaction tx, Guid id, CancellationToken ct)
        {
            await using var cmd = new SqlCommand("dbo.sp_LoanApplication_Exists", cn, tx)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.CommandTimeout = 30;

            cmd.Parameters.Add(new SqlParameter("@LoanApplicationId", SqlDbType.UniqueIdentifier) { Value = id });

            var v = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(v ?? 0) == 1;
        }

        public async Task<Guid> InsertLoanApplic
```

## Review Notes
- Apply only small infrastructure-oriented changes first.
- Do not alter SQL query semantics automatically.
- Do not change stored procedure names or parameters automatically.
- Require manual review before modifying repository logic in production code.