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
    public interface IDashboardReportService
    {
        Task<DashboardReportResult> GetDashboardAsync(ReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportDashboardExcelAsync(ReportExportUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportDashboardCsvAsync(ReportExportUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportDashboardPdfAsync(ReportExportUpdate update, CancellationToken cancellationToken = default);
    }

    public class DashboardReportService(
        IUserRepository _userRepository,
        ISwapRepository _swapRepository,
        IStakeRepository _stakeRepository,
        IPreSaleOrderRepository _preSaleOrderRepository,
        IPreSaleRepository _preSaleRepository,
        IWithdrawalRepository _withdrawalRepository,
        IReportExcelRenderer _excelRenderer,
        IReportCsvRenderer _csvRenderer,
        IReportPdfRenderer _pdfRenderer) : IDashboardReportService, IScopedDependency
    {
        public async Task<DashboardReportResult> GetDashboardAsync(ReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            update ??= new ReportQueryUpdate();
            ReportLimits.Enforce(update);

            var users = await ApplyRange(_userRepository.AsQueryable(), update).ToListAsync();
            var swaps = await ApplyRange(_swapRepository.AsQueryable(), update).ToListAsync();
            var stakes = await ApplyRange(_stakeRepository.AsQueryable(), update).ToListAsync();
            var preSaleOrders = await ApplyRange(_preSaleOrderRepository.AsQueryable(), update).ToListAsync();
            var withdrawals = await ApplyRange(_withdrawalRepository.AsQueryable(), update).ToListAsync();
            var activePreSales = await _preSaleRepository.AsQueryable()
                .Where(x => x.State == PreSaleState.Active)
                .ToListAsync();
            var activeStakes = await _stakeRepository.AsQueryable()
                .Where(x => x.State == StakeState.Active)
                .ToListAsync();

            var purchasedPreSaleOrders = preSaleOrders
                .Where(x => x.State is PreSaleOrderState.InProgress or PreSaleOrderState.Completed)
                .ToList();

            var result = new DashboardReportResult
            {
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                UniqueUsers = users
                    .Select(UserIdentityKey)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                SwapUsdVolume = swaps.Sum(x => UsdValuationHelper.TokenValue(x.SourceAmount, x.SourceTokenPrice)),
                TreasuryProgramUsdVolume = stakes.Sum(x => UsdValuationHelper.TokenValue(x.StartAmount, x.TokenPrice)),
                PreSaleUsdVolume = purchasedPreSaleOrders.Sum(x => UsdValuationHelper.PreSaleUsdtValue(x.ReceivingTokenAmount, x.TokenPreSalePrice)),
                LockedAssetsUsdValue = activeStakes.Sum(x => UsdValuationHelper.TokenValue(x.TokenAmount, x.TokenPrice)),
                TokensSold = purchasedPreSaleOrders.Sum(x => x.ReceivingTokenAmount),
                ActivePreSales = activePreSales.Count,
                ActiveTreasuryContracts = activeStakes.Count,
                ActiveTreasuryPrograms = activeStakes
                    .Select(x => $"{x.TokenSymbol}|{x.TokenNetworkName}|{x.MonthDuration}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                TransactionCounts = BuildTransactionCounts(swaps, preSaleOrders, stakes, withdrawals),
                DailyActivity = BuildActivity(users, swaps, stakes, preSaleOrders, withdrawals, PeriodBucketer.Day),
                WeeklyActivity = BuildActivity(users, swaps, stakes, preSaleOrders, withdrawals, PeriodBucketer.Week),
                MonthlyActivity = BuildActivity(users, swaps, stakes, preSaleOrders, withdrawals, PeriodBucketer.Month)
            };

            return result;
        }

        public async Task<ReportExportResult> ExportDashboardExcelAsync(ReportExportUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _excelRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportDashboardCsvAsync(ReportExportUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.csv",
                ContentType = "text/csv",
                Content = _csvRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportDashboardPdfAsync(ReportExportUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.pdf",
                ContentType = "application/pdf",
                Content = _pdfRenderer.Render(metadata, table)
            };
        }

        private async Task<(ExportMetadata Metadata, ReportTable Table)> BuildExportAsync(ReportExportUpdate update, CancellationToken cancellationToken)
        {
            update ??= new ReportExportUpdate();
            var dashboard = await GetDashboardAsync(update, cancellationToken);
            var metadata = new ExportMetadata
            {
                Title = "Dashboard Report",
                FileName = $"dashboard-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Filters =
                [
                    new ExportFilterText { Label = "From", Value = update.FromMoment == default ? "All" : update.FromMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "To", Value = update.ToMoment == default ? "All" : update.ToMoment.ToString("yyyy-MM-dd") }
                ]
            };

            var table = ToTable(dashboard);
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            return (metadata, table);
        }

        private static ReportTable ToTable(DashboardReportResult dashboard)
        {
            var table = new ReportTable
            {
                Columns =
                [
                    new ReportTableColumn { Header = "Section", Width = 22 },
                    new ReportTableColumn { Header = "Key", Width = 26 },
                    new ReportTableColumn { Header = "State", Width = 20 },
                    new ReportTableColumn { Header = "Value", Width = 24 }
                ],
                Rows =
                [
                    Row("Metric", "UniqueUsers", null, dashboard.UniqueUsers),
                    Row("Metric", "SwapUsdVolume", null, dashboard.SwapUsdVolume),
                    Row("Metric", "TreasuryProgramUsdVolume", null, dashboard.TreasuryProgramUsdVolume),
                    Row("Metric", "PreSaleUsdVolume", null, dashboard.PreSaleUsdVolume),
                    Row("Metric", "LockedAssetsUsdValue", null, dashboard.LockedAssetsUsdValue),
                    Row("Metric", "TokensSold", null, dashboard.TokensSold),
                    Row("Metric", "ActivePreSales", null, dashboard.ActivePreSales),
                    Row("Metric", "ActiveTreasuryPrograms", null, dashboard.ActiveTreasuryPrograms),
                    Row("Metric", "ActiveTreasuryContracts", null, dashboard.ActiveTreasuryContracts)
                ]
            };

            foreach (var count in dashboard.TransactionCounts)
                table.Rows.Add(Row("TransactionCount", count.Type, count.State, count.Count));

            AddActivityRows(table, "DailyActivity", dashboard.DailyActivity);
            AddActivityRows(table, "WeeklyActivity", dashboard.WeeklyActivity);
            AddActivityRows(table, "MonthlyActivity", dashboard.MonthlyActivity);

            return table;
        }

        private static void AddActivityRows(ReportTable table, string section, List<DashboardActivityPointResult> activity)
        {
            foreach (var point in activity)
            {
                table.Rows.Add(Row(section, $"{point.PeriodStart:yyyy-MM-dd} Users", null, point.Users));
                table.Rows.Add(Row(section, $"{point.PeriodStart:yyyy-MM-dd} Swaps", null, point.Swaps));
                table.Rows.Add(Row(section, $"{point.PeriodStart:yyyy-MM-dd} TreasuryPrograms", null, point.TreasuryPrograms));
                table.Rows.Add(Row(section, $"{point.PeriodStart:yyyy-MM-dd} PreSaleOrders", null, point.PreSaleOrders));
                table.Rows.Add(Row(section, $"{point.PeriodStart:yyyy-MM-dd} Withdrawals", null, point.Withdrawals));
            }
        }

        private static IReadOnlyList<object> Row(string section, string key, string state, object value)
            => [section, key, state ?? string.Empty, value];

        private static string UserIdentityKey(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.UserPublicKey))
                return user.UserPublicKey.Trim();

            if (!string.IsNullOrWhiteSpace(user.EVMWalletAddress))
                return user.EVMWalletAddress.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(user.TronWalletAddress))
                return user.TronWalletAddress.Trim().ToLowerInvariant();

            return null;
        }

        private static List<DashboardTransactionCountResult> BuildTransactionCounts(
            List<Swap> swaps,
            List<PreSaleOrder> preSaleOrders,
            List<Stake> stakes,
            List<Withdrawal> withdrawals)
        {
            var counts = new List<DashboardTransactionCountResult>();

            counts.AddRange(swaps
                .GroupBy(x => x.State)
                .Select(x => Count("Swap", x.Key.ToString(), x.Count())));

            counts.AddRange(preSaleOrders
                .GroupBy(x => x.State)
                .Select(x => Count("PreSaleOrder", x.Key.ToString(), x.Count())));

            counts.AddRange(stakes
                .GroupBy(x => x.State)
                .Select(x => Count("Stake", x.Key.ToString(), x.Count())));

            counts.AddRange(withdrawals
                .GroupBy(x => x.State)
                .Select(x => Count("Withdrawal", x.Key.ToString(), x.Count())));

            return counts
                .OrderBy(x => x.Type)
                .ThenBy(x => x.State)
                .ToList();
        }

        private static DashboardTransactionCountResult Count(string type, string state, int count)
            => new()
            {
                Type = type,
                State = state,
                Count = count
            };

        private static List<DashboardActivityPointResult> BuildActivity(
            List<User> users,
            List<Swap> swaps,
            List<Stake> stakes,
            List<PreSaleOrder> preSaleOrders,
            List<Withdrawal> withdrawals,
            Func<DateTime, DateTime> bucket)
        {
            var periodStarts = users.Select(x => bucket(x.CreatedMoment))
                .Concat(swaps.Select(x => bucket(x.CreatedMoment)))
                .Concat(stakes.Select(x => bucket(x.CreatedMoment)))
                .Concat(preSaleOrders.Select(x => bucket(x.CreatedMoment)))
                .Concat(withdrawals.Select(x => bucket(x.CreatedMoment)))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return periodStarts
                .Select(period => new DashboardActivityPointResult
                {
                    PeriodStart = period,
                    Users = users.Count(x => bucket(x.CreatedMoment) == period),
                    Swaps = swaps.Count(x => bucket(x.CreatedMoment) == period),
                    TreasuryPrograms = stakes.Count(x => bucket(x.CreatedMoment) == period),
                    PreSaleOrders = preSaleOrders.Count(x => bucket(x.CreatedMoment) == period),
                    Withdrawals = withdrawals.Count(x => bucket(x.CreatedMoment) == period)
                })
                .ToList();
        }

        private static IMongoQueryable<T> ApplyRange<T>(IMongoQueryable<T> query, ReportQueryUpdate update) where T : global::Utilities.MongoDatabase.Documents.BaseDocument
        {
            if (update.FromMoment != default)
                query = query.Where(x => x.CreatedMoment >= update.FromMoment);

            if (update.ToMoment != default)
                query = query.Where(x => x.CreatedMoment <= update.ToMoment);

            return query;
        }
    }
}
