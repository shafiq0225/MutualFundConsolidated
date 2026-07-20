using MutualFund.Investment.Domain.Entities;

namespace MutualFund.Investment.Application.Statements.Dtos
{
    public static class StatementMapper
    {
        public static InvestmentStatementDto ToDto(
            InvestmentStatement statement)
        {
            return new InvestmentStatementDto
            {
                Id = statement.Id,
                OrderId = statement.OrderId,

                // From navigation property
                OrderNumber = statement.Order?.OrderNumber ?? string.Empty,
                SchemeCode = statement.Order?.SchemeCode ?? string.Empty,
                SchemeName = statement.Order?.SchemeName ?? string.Empty,
                FundName = statement.Order?.FundName ?? string.Empty,

                InvestorUserId = statement.InvestorUserId,
                InvestorName = statement.InvestorName,

                StatementDate = statement.StatementDate,

                FilePath = statement.FilePath,
                FileName = statement.FileName,
                FileSizeBytes = statement.FileSizeBytes,
                FileSizeText = FormatFileSize(statement.FileSizeBytes),

                UploadedByUserId = statement.UploadedByUserId,
                UploadedAt = statement.UploadedAt,
                Notes = statement.Notes
            };
        }

        public static IEnumerable<InvestmentStatementDto> ToDtoList(
            IEnumerable<InvestmentStatement> statements) =>
            statements.Select(ToDto);

        // ── Helper ─────────────────────────────────────────────────
        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F1} MB";
        }
    }
}