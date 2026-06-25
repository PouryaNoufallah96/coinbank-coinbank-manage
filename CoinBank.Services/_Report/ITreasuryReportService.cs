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
    public interface ITreasuryReportService
    {
        Task<TreasuryReportResult> GetTreasuryAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportTreasuryExcelAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportTreasuryCsvAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportTreasuryPdfAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default);
    }

    public class TreasuryReportService(
        IStakeRepository _stakeRepository,
        IReportExcelRenderer _excelRenderer,
        IReportCsvRenderer _csvRenderer,
        IReportPdfRenderer _pdfRenderer) : ITreasuryReportService, IScopedDependency
    {
        public async Task<TreasuryReportResult> GetTreasuryAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            update ??= new TreasuryReportQueryUpdate();
            ReportLimits.Enforce(update);

            var stakes = await GetFilteredStakesAsync(update, cancellationToken);
            var rows = stakes.Select(ToRow).ToList();
            var page = update.Pagination;

            return new TreasuryReportResult
            {
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Summary = BuildSummary(stakes),
                Contracts = new ReportPageResult<TreasuryContractReportRowResult>
                {
                    TotalCount = rows.Count,
                    PageCount = (int)Math.Ceiling(rows.Count / (double)page.Size),
                    Data = rows.Skip((page.Page - 1) * page.Size).Take(page.Size).ToList()
                },
                MaturityWindows = BuildMaturityWindows(stakes),
                FutureMaturitySchedule = BuildFutureMaturitySchedule(stakes)
            };
        }

        public async Task<ReportExportResult> ExportTreasuryExcelAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _excelRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportTreasuryCsvAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.csv",
                ContentType = "text/csv",
                Content = _csvRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportTreasuryPdfAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.pdf",
                ContentType = "application/pdf",
                Content = _pdfRenderer.Render(metadata, table)
            };
        }

        private async Task<(ExportMetadata Metadata, ReportTable Table)> BuildExportAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken)
        {
            update ??= new TreasuryReportQueryUpdate();
            ReportLimits.Enforce(update);

            var stakes = await GetFilteredStakesAsync(update, cancellationToken);

            var metadata = new ExportMetadata
            {
                Title = "Treasury Program Report",
                FileName = $"treasury-program-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Filters =
                [
                    new ExportFilterText { Label = "From", Value = update.FromMoment == default ? "All" : update.FromMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "To", Value = update.ToMoment == default ? "All" : update.ToMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "Wallet", Value = string.IsNullOrWhiteSpace(update.Wallet) ? "All" : update.Wallet },
                    new ExportFilterText { Label = "Token", Value = string.IsNullOrWhiteSpace(update.Token) ? "All" : update.Token },
                    new ExportFilterText { Label = "Status", Value = update.Status?.ToString() ?? "All" }
                ]
            };

            var table = ToTable(stakes);
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            return (metadata, table);
        }

        private async Task<List<Stake>> GetFilteredStakesAsync(TreasuryReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var query = _stakeRepository.AsQueryable();

            if (update.FromMoment != default)
                query = query.Where(x => x.CreatedMoment >= update.FromMoment);

            if (update.ToMoment != default)
                query = query.Where(x => x.CreatedMoment <= update.ToMoment);

            if (update.Status.HasValue)
                query = query.Where(x => x.State == update.Status.Value);

            var stakes = await query.ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(update.Wallet))
            {
                var wallet = update.Wallet.Trim();
                stakes = stakes.Where(x => Contains(x.WalletAddress, wallet)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                stakes = stakes
                    .Where(x => EqualsText(x.TokenSymbol, token) ||
                                Contains(x.TokenName, token) ||
                                EqualsText(x.TokenNetworkName, token))
                    .ToList();
            }

            return stakes
                .OrderByDescending(x => x.CreatedMoment)
                .ThenBy(x => x.StakeReference)
                .ToList();
        }

        private static TreasuryReportSummaryResult BuildSummary(List<Stake> stakes)
        {
            var activeStakes = stakes.Where(IsActive).ToList();

            return new TreasuryReportSummaryResult
            {
                ActiveUserCount = activeStakes
                    .Where(x => !string.IsNullOrWhiteSpace(x.WalletAddress))
                    .Select(x => x.WalletAddress.Trim().ToLowerInvariant())
                    .Distinct()
                    .Count(),
                ActiveContractCount = activeStakes.Count,
                TotalContractCount = stakes.Count,
                TotalHistoricalUsdVolume = stakes.Sum(HistoricalUsdVolume),
                TotalLockedUsdValue = activeStakes.Sum(LockedUsdValue),
                CommittedReward = stakes.Sum(CommittedReward),
                PaidReward = stakes.Sum(x => x.TotalProfitWithdrawn),
                RemainingReward = stakes.Sum(RemainingReward),
                CommittedRewardUsdValue = stakes.Sum(x => UsdValuationHelper.TokenValue(CommittedReward(x), x.TokenPrice)),
                PaidRewardUsdValue = stakes.Sum(x => UsdValuationHelper.TokenValue(x.TotalProfitWithdrawn, x.TokenPrice)),
                RemainingRewardUsdValue = stakes.Sum(RemainingRewardUsdValue)
            };
        }

        private static List<TreasuryMaturityWindowResult> BuildMaturityWindows(List<Stake> stakes)
        {
            var now = DateTime.UtcNow;

            return
            [
                BuildMaturityWindow("30 days", stakes, now, now.AddDays(30)),
                BuildMaturityWindow("90 days", stakes, now, now.AddDays(90))
            ];
        }

        private static TreasuryMaturityWindowResult BuildMaturityWindow(string name, List<Stake> stakes, DateTime from, DateTime to)
        {
            var due = stakes
                .Where(IsActive)
                .Where(x => x.EndMoment >= from && x.EndMoment <= to)
                .ToList();

            return new TreasuryMaturityWindowResult
            {
                Window = name,
                FromMoment = from,
                ToMoment = to,
                ContractCount = due.Count,
                PrincipalDueUsdValue = due.Sum(PrincipalDueUsdValue),
                RewardDueUsdValue = due.Sum(RemainingRewardUsdValue),
                PrincipalAndRewardDueUsdValue = due.Sum(PrincipalAndRewardDueUsdValue)
            };
        }

        private static List<TreasuryMaturityScheduleResult> BuildFutureMaturitySchedule(List<Stake> stakes)
        {
            var now = DateTime.UtcNow;

            return stakes
                .Where(IsActive)
                .Where(x => x.EndMoment >= now)
                .GroupBy(x => PeriodBucketer.Month(x.EndMoment))
                .OrderBy(x => x.Key)
                .Select(x => new TreasuryMaturityScheduleResult
                {
                    PeriodStart = x.Key,
                    ContractCount = x.Count(),
                    PrincipalDueUsdValue = x.Sum(PrincipalDueUsdValue),
                    RewardDueUsdValue = x.Sum(RemainingRewardUsdValue),
                    PrincipalAndRewardDueUsdValue = x.Sum(PrincipalAndRewardDueUsdValue)
                })
                .ToList();
        }

        private static ReportTable ToTable(List<Stake> stakes)
        {
            var summary = BuildSummary(stakes);
            var table = new ReportTable
            {
                Columns =
                [
                    new ReportTableColumn { Header = "Section", Width = 18 },
                    new ReportTableColumn { Header = "Date", Width = 20 },
                    new ReportTableColumn { Header = "Reference", Width = 24 },
                    new ReportTableColumn { Header = "Wallet", Width = 34 },
                    new ReportTableColumn { Header = "Token", Width = 16 },
                    new ReportTableColumn { Header = "Network", Width = 18 },
                    new ReportTableColumn { Header = "StartAmount", Width = 18 },
                    new ReportTableColumn { Header = "TokenAmount", Width = 18 },
                    new ReportTableColumn { Header = "TokenPrice", Width = 14 },
                    new ReportTableColumn { Header = "UsdValue", Width = 18 },
                    new ReportTableColumn { Header = "CommittedReward", Width = 18 },
                    new ReportTableColumn { Header = "PaidReward", Width = 18 },
                    new ReportTableColumn { Header = "RemainingReward", Width = 18 },
                    new ReportTableColumn { Header = "DueUsd", Width = 18 },
                    new ReportTableColumn { Header = "State", Width = 14 }
                ],
                Rows = []
            };

            table.Rows.Add(Row("Summary", string.Empty, "ActiveUsers", string.Empty, string.Empty, string.Empty, string.Empty, summary.ActiveUserCount, string.Empty, summary.TotalLockedUsdValue, summary.CommittedReward, summary.PaidReward, summary.RemainingReward, string.Empty, summary.ActiveContractCount));
            table.Rows.Add(Row("Summary", string.Empty, "HistoricalVolume", string.Empty, string.Empty, string.Empty, string.Empty, summary.TotalContractCount, string.Empty, summary.TotalHistoricalUsdVolume, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var window in BuildMaturityWindows(stakes))
                table.Rows.Add(Row("MaturityWindow", $"{window.FromMoment:yyyy-MM-dd} - {window.ToMoment:yyyy-MM-dd}", window.Window, string.Empty, string.Empty, string.Empty, string.Empty, window.ContractCount, string.Empty, window.PrincipalDueUsdValue, string.Empty, string.Empty, window.RewardDueUsdValue, window.PrincipalAndRewardDueUsdValue, string.Empty));

            foreach (var schedule in BuildFutureMaturitySchedule(stakes))
                table.Rows.Add(Row("FutureMaturity", schedule.PeriodStart.ToString("yyyy-MM"), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, schedule.ContractCount, string.Empty, schedule.PrincipalDueUsdValue, string.Empty, string.Empty, schedule.RewardDueUsdValue, schedule.PrincipalAndRewardDueUsdValue, string.Empty));

            foreach (var stake in stakes)
            {
                var row = ToRow(stake);
                table.Rows.Add(Row(
                    "Contract",
                    row.CreatedMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                    row.StakeReference,
                    row.WalletAddress,
                    row.TokenSymbol,
                    row.TokenNetworkName,
                    row.StartAmount,
                    row.TokenAmount,
                    row.TokenPrice,
                    row.LockedUsdValue,
                    row.CommittedReward,
                    row.PaidReward,
                    row.RemainingReward,
                    row.PrincipalAndRewardDueUsdValue,
                    row.State));
            }

            return table;
        }

        private static TreasuryContractReportRowResult ToRow(Stake stake)
            => new()
            {
                CreatedMoment = stake.CreatedMoment,
                StakeReference = stake.StakeReference,
                WalletAddress = stake.WalletAddress,
                TokenSymbol = stake.TokenSymbol,
                TokenName = stake.TokenName,
                TokenNetworkName = stake.TokenNetworkName,
                StartAmount = stake.StartAmount,
                TokenAmount = stake.TokenAmount,
                TokenPrice = stake.TokenPrice,
                HistoricalUsdVolume = HistoricalUsdVolume(stake),
                LockedUsdValue = IsActive(stake) ? LockedUsdValue(stake) : 0,
                EachMonthProfit = stake.EachMonthProfit,
                MonthDuration = stake.MonthDuration,
                CommittedReward = CommittedReward(stake),
                PaidReward = stake.TotalProfitWithdrawn,
                RemainingReward = RemainingReward(stake),
                RemainingRewardUsdValue = RemainingRewardUsdValue(stake),
                PrincipalDue = PrincipalDue(stake),
                PrincipalDueUsdValue = PrincipalDueUsdValue(stake),
                PrincipalAndRewardDueUsdValue = PrincipalAndRewardDueUsdValue(stake),
                StartMoment = stake.StartMoment,
                EndMoment = stake.EndMoment,
                State = stake.State.ToString(),
                RegisterHash = stake.RegisterHash,
                RegisterMoment = stake.RegisterMoment
            };

        private static bool IsActive(Stake stake)
            => stake.State == StakeState.Active;

        private static decimal HistoricalUsdVolume(Stake stake)
            => UsdValuationHelper.TokenValue(stake.StartAmount, stake.TokenPrice);

        private static decimal LockedUsdValue(Stake stake)
            => UsdValuationHelper.TokenValue(stake.TokenAmount, stake.TokenPrice);

        private static decimal CommittedReward(Stake stake)
            => stake.EachMonthProfit * stake.MonthDuration;

        private static decimal RemainingReward(Stake stake)
            => IsActive(stake) ? ObligationCalculator.RemainingStakeProfit(CommittedReward(stake), stake.TotalProfitWithdrawn) : 0;

        private static decimal RemainingRewardUsdValue(Stake stake)
            => UsdValuationHelper.TokenValue(RemainingReward(stake), stake.TokenPrice);

        private static decimal PrincipalDue(Stake stake)
            => IsActive(stake) ? stake.TokenAmount : 0;

        private static decimal PrincipalDueUsdValue(Stake stake)
            => UsdValuationHelper.TokenValue(PrincipalDue(stake), stake.TokenPrice);

        private static decimal PrincipalAndRewardDueUsdValue(Stake stake)
            => PrincipalDueUsdValue(stake) + RemainingRewardUsdValue(stake);

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
