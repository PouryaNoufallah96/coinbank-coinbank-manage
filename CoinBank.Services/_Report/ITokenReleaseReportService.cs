using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using CoinBank.Services._Report.Calculators;
using CoinBank.Services._Report.DTOs.Results;
using CoinBank.Services._Report.DTOs.Updates;
using CoinBank.Services._Report.Exports;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Services._Report
{
    public interface ITokenReleaseReportService
    {
        Task<TokenReleaseReportResult> GetTokenReleasesAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportTokenReleasesExcelAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportTokenReleasesCsvAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportTokenReleasesPdfAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default);
    }

    public class TokenReleaseReportService(
        IPreSaleOrderRepository _preSaleOrderRepository,
        ITransactionLogRepository _transactionLogRepository,
        IReportExcelRenderer _excelRenderer,
        IReportCsvRenderer _csvRenderer,
        IReportPdfRenderer _pdfRenderer) : ITokenReleaseReportService, IScopedDependency
    {
        private const string Scheduled = "Scheduled";
        private const string Submitted = "Submitted";
        private const string Confirmed = "Confirmed";

        public async Task<TokenReleaseReportResult> GetTokenReleasesAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            update ??= new TokenReleaseReportQueryUpdate();
            ReportLimits.Enforce(update);

            var rows = await GetFilteredRowsAsync(update, cancellationToken);
            var page = update.Pagination;

            return new TokenReleaseReportResult
            {
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Summary = BuildSummary(rows),
                Releases = new ReportPageResult<TokenReleaseReportRowResult>
                {
                    TotalCount = rows.Count,
                    PageCount = (int)Math.Ceiling(rows.Count / (double)page.Size),
                    Data = rows.Skip((page.Page - 1) * page.Size).Take(page.Size).ToList()
                },
                ReleaseWindows = BuildReleaseWindows(rows),
                ReleaseCalendar = BuildReleaseCalendar(rows)
            };
        }

        public async Task<ReportExportResult> ExportTokenReleasesExcelAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _excelRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportTokenReleasesCsvAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.csv",
                ContentType = "text/csv",
                Content = _csvRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportTokenReleasesPdfAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.pdf",
                ContentType = "application/pdf",
                Content = _pdfRenderer.Render(metadata, table)
            };
        }

        private async Task<(ExportMetadata Metadata, ReportTable Table)> BuildExportAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken)
        {
            update ??= new TokenReleaseReportQueryUpdate();
            ReportLimits.Enforce(update);

            var rows = await GetFilteredRowsAsync(update, cancellationToken);

            var metadata = new ExportMetadata
            {
                Title = "Token Release Report",
                FileName = $"token-release-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Filters =
                [
                    new ExportFilterText { Label = "From", Value = update.FromMoment == default ? "All" : update.FromMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "To", Value = update.ToMoment == default ? "All" : update.ToMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "Wallet", Value = string.IsNullOrWhiteSpace(update.Wallet) ? "All" : update.Wallet },
                    new ExportFilterText { Label = "Token", Value = string.IsNullOrWhiteSpace(update.Token) ? "All" : update.Token },
                    new ExportFilterText { Label = "Status", Value = string.IsNullOrWhiteSpace(update.Status) ? "All" : update.Status }
                ]
            };

            var table = ToTable(rows);
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            return (metadata, table);
        }

        private async Task<List<TokenReleaseReportRowResult>> GetFilteredRowsAsync(TokenReleaseReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var query = _preSaleOrderRepository.AsQueryable()
                .Where(x => x.ReleaseSchedule != null && x.ReleaseSchedule.Any());

            if (update.FromMoment != default)
                query = query.Where(x => x.ReleaseSchedule.Any(r => r.ReleaseDate >= update.FromMoment));

            if (update.ToMoment != default)
                query = query.Where(x => x.ReleaseSchedule.Any(r => r.ReleaseDate <= update.ToMoment));

            var orders = await query.ToListAsync(cancellationToken);
            var confirmedLogs = await GetConfirmedReleaseLogsAsync(cancellationToken);

            var rows = orders
                .SelectMany(order => order.ReleaseSchedule.Select(step => ToRow(order, step, confirmedLogs)))
                .ToList();

            if (update.FromMoment != default)
                rows = rows.Where(x => x.ReleaseDate >= update.FromMoment).ToList();

            if (update.ToMoment != default)
                rows = rows.Where(x => x.ReleaseDate <= update.ToMoment).ToList();

            if (!string.IsNullOrWhiteSpace(update.Wallet))
            {
                var wallet = update.Wallet.Trim();
                rows = rows
                    .Where(x => Contains(x.WalletAddress, wallet) ||
                                Contains(x.UserPublicKey, wallet))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                rows = rows
                    .Where(x => EqualsText(x.TokenSymbol, token) ||
                                Contains(x.TokenName, token))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Status))
            {
                var status = update.Status.Trim();
                rows = rows
                    .Where(x => EqualsText(x.Status, status))
                    .ToList();
            }

            return rows
                .OrderBy(x => x.ReleaseDate)
                .ThenBy(x => x.PreSaleOrderReference)
                .ThenBy(x => x.Percentage)
                .ToList();
        }

        private async Task<List<TransactionLog>> GetConfirmedReleaseLogsAsync(CancellationToken cancellationToken)
        {
            return await _transactionLogRepository.AsQueryable()
                .Where(x => x.EventType == BlockchainEventType.PreSaleReleaseClaimed &&
                            x.Status == TransactionStatus.Confirmed)
                .ToListAsync(cancellationToken);
        }

        private static TokenReleaseReportRowResult ToRow(
            PreSaleOrder order,
            PreSaleOrderReleaseStep step,
            List<TransactionLog> confirmedLogs)
        {
            var confirmedLog = FindConfirmedLog(order, step, confirmedLogs);

            return new TokenReleaseReportRowResult
            {
                CreatedMoment = order.CreatedMoment,
                PreSaleOrderReference = order.PreSaleOrderReference,
                PreSaleReference = order.PreSaleReference,
                TokenSymbol = order.Symbol,
                TokenName = order.Name,
                WalletAddress = order.WalletAddress,
                UserPublicKey = order.UserPublicKey,
                Percentage = step.Percentage,
                Amount = ReleaseAmount(order, step),
                ReleaseDate = step.ReleaseDate,
                Status = confirmedLog != null ? Confirmed : IsSubmitted(step) ? Submitted : Scheduled,
                RegisterHash = step.RegisterHash,
                RegisterMoment = step.RegisterMoment,
                ConfirmedHash = confirmedLog?.Hash,
                ConfirmedMoment = confirmedLog?.CreatedMoment
            };
        }

        private static TransactionLog FindConfirmedLog(
            PreSaleOrder order,
            PreSaleOrderReleaseStep step,
            List<TransactionLog> confirmedLogs)
        {
            if (string.IsNullOrWhiteSpace(step.RegisterHash))
                return null;

            return confirmedLogs.FirstOrDefault(x =>
                EqualsText(x.Hash, step.RegisterHash) &&
                EqualsText(x.Reference, order.PreSaleOrderReference));
        }

        private static decimal ReleaseAmount(PreSaleOrder order, PreSaleOrderReleaseStep step)
        {
            if (step.CliamedAmount.HasValue && step.CliamedAmount.Value > 0)
                return step.CliamedAmount.Value;

            return order.ReceivingTokenAmount * step.Percentage / 100;
        }

        private static bool IsSubmitted(PreSaleOrderReleaseStep step)
            => step.RegisterMoment.HasValue ||
               !string.IsNullOrWhiteSpace(step.RegisterHash) ||
               (step.CliamedAmount.HasValue && step.CliamedAmount.Value > 0);

        private static TokenReleaseReportSummaryResult BuildSummary(List<TokenReleaseReportRowResult> rows)
            => new()
            {
                ReleaseCount = rows.Count,
                TotalAmount = rows.Sum(x => x.Amount),
                ScheduledCount = rows.Count(x => x.Status == Scheduled),
                ScheduledAmount = rows.Where(x => x.Status == Scheduled).Sum(x => x.Amount),
                SubmittedCount = rows.Count(x => x.Status == Submitted),
                SubmittedAmount = rows.Where(x => x.Status == Submitted).Sum(x => x.Amount),
                ConfirmedCount = rows.Count(x => x.Status == Confirmed),
                ConfirmedAmount = rows.Where(x => x.Status == Confirmed).Sum(x => x.Amount)
            };

        private static List<TokenReleaseWindowResult> BuildReleaseWindows(List<TokenReleaseReportRowResult> rows)
        {
            var now = DateTime.UtcNow;

            return
            [
                BuildReleaseWindow("30 days", rows, now, now.AddDays(30)),
                BuildReleaseWindow("90 days", rows, now, now.AddDays(90))
            ];
        }

        private static TokenReleaseWindowResult BuildReleaseWindow(string name, List<TokenReleaseReportRowResult> rows, DateTime from, DateTime to)
        {
            var due = rows
                .Where(x => x.ReleaseDate >= from && x.ReleaseDate <= to)
                .ToList();

            return new TokenReleaseWindowResult
            {
                Window = name,
                FromMoment = from,
                ToMoment = to,
                ReleaseCount = due.Count,
                Amount = due.Sum(x => x.Amount),
                ScheduledCount = due.Count(x => x.Status == Scheduled),
                ScheduledAmount = due.Where(x => x.Status == Scheduled).Sum(x => x.Amount),
                SubmittedCount = due.Count(x => x.Status == Submitted),
                SubmittedAmount = due.Where(x => x.Status == Submitted).Sum(x => x.Amount),
                ConfirmedCount = due.Count(x => x.Status == Confirmed),
                ConfirmedAmount = due.Where(x => x.Status == Confirmed).Sum(x => x.Amount)
            };
        }

        private static List<TokenReleaseCalendarResult> BuildReleaseCalendar(List<TokenReleaseReportRowResult> rows)
        {
            return rows
                .GroupBy(x => PeriodBucketer.Month(x.ReleaseDate))
                .OrderBy(x => x.Key)
                .Select(x => new TokenReleaseCalendarResult
                {
                    PeriodStart = x.Key,
                    ReleaseCount = x.Count(),
                    Amount = x.Sum(r => r.Amount),
                    ScheduledCount = x.Count(r => r.Status == Scheduled),
                    ScheduledAmount = x.Where(r => r.Status == Scheduled).Sum(r => r.Amount),
                    SubmittedCount = x.Count(r => r.Status == Submitted),
                    SubmittedAmount = x.Where(r => r.Status == Submitted).Sum(r => r.Amount),
                    ConfirmedCount = x.Count(r => r.Status == Confirmed),
                    ConfirmedAmount = x.Where(r => r.Status == Confirmed).Sum(r => r.Amount)
                })
                .ToList();
        }

        private static ReportTable ToTable(List<TokenReleaseReportRowResult> rows)
        {
            var summary = BuildSummary(rows);
            var table = new ReportTable
            {
                Columns =
                [
                    new ReportTableColumn { Header = "Section", Width = 18 },
                    new ReportTableColumn { Header = "Date", Width = 20 },
                    new ReportTableColumn { Header = "OrderReference", Width = 24 },
                    new ReportTableColumn { Header = "Wallet", Width = 34 },
                    new ReportTableColumn { Header = "Token", Width = 16 },
                    new ReportTableColumn { Header = "Percentage", Width = 14 },
                    new ReportTableColumn { Header = "Amount", Width = 18 },
                    new ReportTableColumn { Header = "Status", Width = 14 },
                    new ReportTableColumn { Header = "RegisterHash", Width = 34 },
                    new ReportTableColumn { Header = "RegisterMoment", Width = 20 },
                    new ReportTableColumn { Header = "ConfirmedHash", Width = 34 },
                    new ReportTableColumn { Header = "ConfirmedMoment", Width = 20 }
                ],
                Rows = []
            };

            table.Rows.Add(Row("Summary", string.Empty, "Total", string.Empty, string.Empty, string.Empty, summary.TotalAmount, summary.ReleaseCount, string.Empty, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", string.Empty, Scheduled, string.Empty, string.Empty, string.Empty, summary.ScheduledAmount, summary.ScheduledCount, string.Empty, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", string.Empty, Submitted, string.Empty, string.Empty, string.Empty, summary.SubmittedAmount, summary.SubmittedCount, string.Empty, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", string.Empty, Confirmed, string.Empty, string.Empty, string.Empty, summary.ConfirmedAmount, summary.ConfirmedCount, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var window in BuildReleaseWindows(rows))
                table.Rows.Add(Row("Window", $"{window.FromMoment:yyyy-MM-dd} - {window.ToMoment:yyyy-MM-dd}", window.Window, string.Empty, string.Empty, string.Empty, window.Amount, window.ReleaseCount, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var calendar in BuildReleaseCalendar(rows))
                table.Rows.Add(Row("Calendar", calendar.PeriodStart.ToString("yyyy-MM"), string.Empty, string.Empty, string.Empty, string.Empty, calendar.Amount, calendar.ReleaseCount, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var row in rows)
            {
                table.Rows.Add(Row(
                    "Release",
                    row.ReleaseDate.ToString("yyyy-MM-dd"),
                    row.PreSaleOrderReference,
                    row.WalletAddress,
                    row.TokenSymbol,
                    row.Percentage,
                    row.Amount,
                    row.Status,
                    row.RegisterHash,
                    row.RegisterMoment?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                    row.ConfirmedHash,
                    row.ConfirmedMoment?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty));
            }

            return table;
        }

        private static IReadOnlyList<object> Row(params object[] values)
            => values;

        private static bool Contains(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);

        private static bool EqualsText(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               string.Equals(source.Trim(), value, StringComparison.OrdinalIgnoreCase);
    }
}
