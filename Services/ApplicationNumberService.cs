using Claytree.Risk.Functions.Interface;
using Claytree.Risk.Functions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Claytree.Risk.Functions.Services
{
    public sealed class ApplicationNumberService : IApplicationNumberService
    {
        private readonly ILoanRepository _repo;
        private readonly ApplicationNumberOptions _opt;

        public ApplicationNumberService(ILoanRepository repo, IOptions<ApplicationNumberOptions> opt)
        {
            _repo = repo;
            _opt = opt.Value;
        }

        public async Task<string> EnsureAsync(SqlConnection cn, SqlTransaction tx,
            Guid loanApplicationId, Guid? tenantId,
            string? branchCode, string? productCode,
            string? requestedApplicationNumber,
            CancellationToken ct)
        {
            // If caller provided appNo, set if empty
            var req = string.IsNullOrWhiteSpace(requestedApplicationNumber) ? null : Sanitize(requestedApplicationNumber!);
            if (!string.IsNullOrWhiteSpace(req))
                return await _repo.SetApplicationNumberIfEmptyAsync(cn, tx, loanApplicationId, req!, ct);

            if (!_opt.Enabled) return "";

            var fmt = string.IsNullOrWhiteSpace(_opt.DefaultFormat)
                ? "{BRANCH:3}-{PRODUCT:2}-{YY}{MM}-{SERIAL:6}"
                : _opt.DefaultFormat;

            var branch = Normalize(branchCode);
            var product = Normalize(productCode);

            if (string.IsNullOrWhiteSpace(branch)) branch = Normalize(_opt.DefaultBranchCode);
            if (string.IsNullOrWhiteSpace(product)) product = Normalize(_opt.DefaultProductCode);

            if (branch.Length < 3) branch = branch.PadRight(3, 'X');
            if (product.Length < 2) product = product.PadRight(2, 'X');

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                var now = DateTime.UtcNow;
                var yymm = now.ToString("yyMM");

                var tail = _opt.Mode.Equals("RANDOM", StringComparison.OrdinalIgnoreCase)
                    ? RandomDigits(6)
                    : await _repo.AllocateSerialAsync(cn, tx, tenantId, branch, product, yymm, 6, ct);

                var appNo = Format(fmt, branch, product, now, tail);

                try
                {
                    var final = await _repo.SetApplicationNumberIfEmptyAsync(cn, tx, loanApplicationId, appNo, ct);
                    return string.IsNullOrWhiteSpace(final) ? appNo : final;
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    // Unique collision retry
                }
            }

            throw new InvalidOperationException("Unable to generate unique ApplicationNumber after retries.");
        }

        private static string Format(string fmt, string branch, string product, DateTime now, string tail)
        {
            var res = fmt;
            res = ReplaceLen(res, "BRANCH", branch);
            res = ReplaceLen(res, "PRODUCT", product);

            res = res.Replace("{YY}", now.ToString("yy"))
                     .Replace("{YYYY}", now.ToString("yyyy"))
                     .Replace("{MM}", now.ToString("MM"))
                     .Replace("{YYMM}", now.ToString("yyMM"));

            res = ReplaceNum(res, "SERIAL", tail);
            res = ReplaceNum(res, "RANDOM", tail);

            return Sanitize(res);
        }

        private static string ReplaceLen(string input, string token, string value)
        {
            while (true)
            {
                var start = input.IndexOf("{" + token + ":", StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;
                var end = input.IndexOf("}", start, StringComparison.Ordinal);
                if (end < 0) break;

                var inside = input.Substring(start + token.Length + 2, end - (start + token.Length + 2));
                _ = int.TryParse(inside, out int n);
                if (n <= 0) n = value.Length;

                var v = value.Length >= n ? value.Substring(0, n) : value.PadRight(n, 'X');
                input = input.Substring(0, start) + v + input.Substring(end + 1);
            }
            return input;
        }

        private static string ReplaceNum(string input, string token, string digits)
        {
            while (true)
            {
                var start = input.IndexOf("{" + token + ":", StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;
                var end = input.IndexOf("}", start, StringComparison.Ordinal);
                if (end < 0) break;

                var inside = input.Substring(start + token.Length + 2, end - (start + token.Length + 2));
                _ = int.TryParse(inside, out int n);
                if (n <= 0) n = digits.Length;

                var v = digits.Length >= n ? digits.Substring(digits.Length - n, n) : digits.PadLeft(n, '0');
                input = input.Substring(0, start) + v + input.Substring(end + 1);
            }
            return input;
        }

        private static string Normalize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        private static string Sanitize(string s)
            => new string(s.ToUpperInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == '-').ToArray());

        private static string RandomDigits(int count)
        {
            Span<byte> bytes = stackalloc byte[count];
            RandomNumberGenerator.Fill(bytes);
            var chars = new char[count];
            for (int i = 0; i < count; i++) chars[i] = (char)('0' + (bytes[i] % 10));
            return new string(chars);
        }
    }
}