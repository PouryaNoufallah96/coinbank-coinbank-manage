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
    public interface IUserReportService
    {
        Task<UserReportResult> GetUsersAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportUsersExcelAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportUsersCsvAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportUsersPdfAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default);
    }

    public class UserReportService(
        IUserRepository _userRepository,
        ISwapRepository _swapRepository,
        IStakeRepository _stakeRepository,
        IPreSaleOrderRepository _preSaleOrderRepository,
        ITransactionLogRepository _transactionLogRepository,
        IReportExcelRenderer _excelRenderer,
        IReportCsvRenderer _csvRenderer,
        IReportPdfRenderer _pdfRenderer) : IUserReportService, IScopedDependency
    {
        private const string SwapActivity = "Swap";
        private const string StakeActivity = "Stake";
        private const string PreSalePurchaseActivity = "PreSalePurchase";
        private const string ConfirmedReleaseActivity = "ConfirmedRelease";

        public async Task<UserReportResult> GetUsersAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            update ??= new UserReportQueryUpdate();
            ReportLimits.Enforce(update);

            var data = await LoadDataAsync(cancellationToken);
            var identities = BuildIdentities(data);
            var walletRows = BuildWalletRows(identities, data, update);
            var historyRows = BuildHistoryRows(identities, data, update);
            var page = update.Pagination;

            return new UserReportResult
            {
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Summary = BuildSummary(walletRows),
                Wallets = new ReportPageResult<UserReportWalletResult>
                {
                    TotalCount = walletRows.Count,
                    PageCount = (int)Math.Ceiling(walletRows.Count / (double)page.Size),
                    Data = walletRows.Skip((page.Page - 1) * page.Size).Take(page.Size).ToList()
                },
                History = new ReportPageResult<UserReportHistoryResult>
                {
                    TotalCount = historyRows.Count,
                    PageCount = (int)Math.Ceiling(historyRows.Count / (double)page.Size),
                    Data = historyRows.Skip((page.Page - 1) * page.Size).Take(page.Size).ToList()
                }
            };
        }

        public async Task<ReportExportResult> ExportUsersExcelAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _excelRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportUsersCsvAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.csv",
                ContentType = "text/csv",
                Content = _csvRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportUsersPdfAsync(UserReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.pdf",
                ContentType = "application/pdf",
                Content = _pdfRenderer.Render(metadata, table)
            };
        }

        private async Task<(ExportMetadata Metadata, ReportTable Table)> BuildExportAsync(UserReportQueryUpdate update, CancellationToken cancellationToken)
        {
            update ??= new UserReportQueryUpdate();
            ReportLimits.Enforce(update);

            var data = await LoadDataAsync(cancellationToken);
            var identities = BuildIdentities(data);
            var walletRows = BuildWalletRows(identities, data, update);
            var historyRows = BuildHistoryRows(identities, data, update);

            var metadata = new ExportMetadata
            {
                Title = "User Management Report",
                FileName = $"user-management-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Filters =
                [
                    new ExportFilterText { Label = "From", Value = update.FromMoment == default ? "All" : update.FromMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "To", Value = update.ToMoment == default ? "All" : update.ToMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "Wallet", Value = string.IsNullOrWhiteSpace(update.Wallet) ? "All" : update.Wallet },
                    new ExportFilterText { Label = "UserPublicKey", Value = string.IsNullOrWhiteSpace(update.UserPublicKey) ? "All" : update.UserPublicKey }
                ]
            };

            var table = ToTable(walletRows, historyRows);
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            return (metadata, table);
        }

        private async Task<UserReportData> LoadDataAsync(CancellationToken cancellationToken)
        {
            var usersTask = _userRepository.AsQueryable().ToListAsync(cancellationToken);
            var swapsTask = _swapRepository.AsQueryable().ToListAsync(cancellationToken);
            var stakesTask = _stakeRepository.AsQueryable().ToListAsync(cancellationToken);
            var preSaleOrdersTask = _preSaleOrderRepository.AsQueryable().ToListAsync(cancellationToken);
            var confirmedReleaseLogsTask = _transactionLogRepository.AsQueryable()
                .Where(x => x.EventType == BlockchainEventType.PreSaleReleaseClaimed &&
                            x.Status == TransactionStatus.Confirmed)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(usersTask, swapsTask, stakesTask, preSaleOrdersTask, confirmedReleaseLogsTask);

            return new UserReportData
            {
                Users = usersTask.Result,
                Swaps = swapsTask.Result,
                Stakes = stakesTask.Result,
                PreSaleOrders = preSaleOrdersTask.Result,
                ConfirmedReleaseLogs = confirmedReleaseLogsTask.Result
            };
        }

        private static List<UserIdentity> BuildIdentities(UserReportData data)
        {
            var identities = data.Users.Select(x => new UserIdentity
            {
                UserPublicKey = x.UserPublicKey,
                EVMWalletAddress = x.EVMWalletAddress,
                TronWalletAddress = x.TronWalletAddress,
                UserCreatedMoment = x.CreatedMoment
            }).ToList();

            foreach (var swap in data.Swaps)
                AddIdentityIfMissing(identities, swap.UserPublicKey, swap.WalletAddress);

            foreach (var stake in data.Stakes)
                AddIdentityIfMissing(identities, null, stake.WalletAddress);

            foreach (var order in data.PreSaleOrders)
                AddIdentityIfMissing(identities, order.UserPublicKey, order.WalletAddress);

            return identities;
        }

        private static List<UserReportWalletResult> BuildWalletRows(List<UserIdentity> identities, UserReportData data, UserReportQueryUpdate update)
        {
            return identities
                .Where(x => MatchesFilter(x, update))
                .Select(x =>
                {
                    var allActivities = BuildAllActivityRows([x], data);
                    var filteredActivities = allActivities
                        .Where(a => IsInRange(a.ActivityMoment, update))
                        .Where(a => MatchesActivityFilter(a, update))
                        .ToList();

                    return ToWalletRow(x, allActivities, filteredActivities);
                })
                .Where(x => x.FirstActivityMoment != default)
                .Where(x => x.TotalActivityCount > 0 || IsInRange(x.FirstActivityMoment, update))
                .OrderByDescending(x => x.FirstActivityMoment)
                .ThenBy(x => x.UserPublicKey)
                .ThenBy(x => x.EVMWalletAddress)
                .ToList();
        }

        private static List<UserReportHistoryResult> BuildHistoryRows(List<UserIdentity> identities, UserReportData data, UserReportQueryUpdate update)
        {
            return BuildAllActivityRows(identities.Where(x => MatchesFilter(x, update)).ToList(), data)
                .Where(x => IsInRange(x.ActivityMoment, update))
                .Where(x => MatchesActivityFilter(x, update))
                .OrderByDescending(x => x.ActivityMoment)
                .ThenBy(x => x.ActivityType)
                .ThenBy(x => x.Reference)
                .ToList();
        }

        private static UserReportWalletResult ToWalletRow(
            UserIdentity identity,
            List<UserReportHistoryResult> allActivities,
            List<UserReportHistoryResult> filteredActivities)
        {
            var swaps = filteredActivities.Where(x => x.ActivityType == SwapActivity).ToList();
            var stakes = filteredActivities.Where(x => x.ActivityType == StakeActivity).ToList();
            var purchases = filteredActivities.Where(x => x.ActivityType == PreSalePurchaseActivity).ToList();
            var releases = filteredActivities.Where(x => x.ActivityType == ConfirmedReleaseActivity).ToList();

            var firstActivityMoments = allActivities.Select(x => x.ActivityMoment).ToList();
            if (identity.UserCreatedMoment.HasValue)
                firstActivityMoments.Add(identity.UserCreatedMoment.Value);

            return new UserReportWalletResult
            {
                UserPublicKey = identity.UserPublicKey,
                EVMWalletAddress = identity.EVMWalletAddress,
                TronWalletAddress = identity.TronWalletAddress,
                FirstActivityMoment = firstActivityMoments.DefaultIfEmpty().Min(),
                SwapCount = swaps.Count,
                SwapUsdVolume = swaps.Sum(x => x.UsdValue ?? 0),
                StakeCount = stakes.Count,
                StakeUsdVolume = stakes.Sum(x => x.UsdValue ?? 0),
                CurrentLockedUsdValue = stakes.Sum(x => x.LockedUsdValue ?? 0),
                PreSalePurchaseCount = purchases.Count,
                PreSaleSalesUsdValue = purchases.Sum(x => x.UsdValue ?? 0),
                ConfirmedReleaseCount = releases.Count,
                ConfirmedReleaseAmount = releases.Sum(x => x.NativeAmount),
                TotalActivityCount = filteredActivities.Count
            };
        }

        private static UserReportSummaryResult BuildSummary(List<UserReportWalletResult> rows)
            => new()
            {
                UserCount = rows.Count,
                EvmWalletCount = rows.Count(x => !string.IsNullOrWhiteSpace(x.EVMWalletAddress)),
                TronWalletCount = rows.Count(x => !string.IsNullOrWhiteSpace(x.TronWalletAddress)),
                SwapCount = rows.Sum(x => x.SwapCount),
                SwapUsdVolume = rows.Sum(x => x.SwapUsdVolume),
                StakeCount = rows.Sum(x => x.StakeCount),
                StakeUsdVolume = rows.Sum(x => x.StakeUsdVolume),
                CurrentLockedUsdValue = rows.Sum(x => x.CurrentLockedUsdValue),
                PreSalePurchaseCount = rows.Sum(x => x.PreSalePurchaseCount),
                PreSaleSalesUsdValue = rows.Sum(x => x.PreSaleSalesUsdValue),
                ConfirmedReleaseCount = rows.Sum(x => x.ConfirmedReleaseCount),
                ConfirmedReleaseAmount = rows.Sum(x => x.ConfirmedReleaseAmount)
            };

        private static List<UserReportHistoryResult> BuildAllActivityRows(List<UserIdentity> identities, UserReportData data)
        {
            var rows = new List<UserReportHistoryResult>();

            rows.AddRange(data.Swaps
                .Where(x => identities.Any(i => Matches(i, x.UserPublicKey, x.WalletAddress)))
                .Select(x => new UserReportHistoryResult
                {
                    ActivityMoment = x.CreatedMoment,
                    ActivityType = SwapActivity,
                    Reference = x.SwapReference,
                    UserPublicKey = x.UserPublicKey,
                    WalletAddress = x.WalletAddress,
                    TokenSymbol = x.SourceSymbol,
                    NativeAmount = x.SourceAmount,
                    UsdValue = UsdValuationHelper.TokenValue(x.SourceAmount, x.SourceTokenPrice),
                    State = x.State.ToString(),
                    Hash = x.RegisterHash
                }));

            rows.AddRange(data.Stakes
                .Where(x => identities.Any(i => Matches(i, null, x.WalletAddress)))
                .Select(x => new UserReportHistoryResult
                {
                    ActivityMoment = x.CreatedMoment,
                    ActivityType = StakeActivity,
                    Reference = x.StakeReference,
                    UserPublicKey = identities.FirstOrDefault(i => Matches(i, null, x.WalletAddress))?.UserPublicKey,
                    WalletAddress = x.WalletAddress,
                    TokenSymbol = x.TokenSymbol,
                    NativeAmount = x.StartAmount,
                    UsdValue = UsdValuationHelper.TokenValue(x.StartAmount, x.TokenPrice),
                    LockedUsdValue = x.State == StakeState.Active ? UsdValuationHelper.TokenValue(x.TokenAmount, x.TokenPrice) : 0,
                    State = x.State.ToString(),
                    Hash = x.RegisterHash
                }));

            rows.AddRange(data.PreSaleOrders
                .Where(x => identities.Any(i => Matches(i, x.UserPublicKey, x.WalletAddress)))
                .Select(x => new UserReportHistoryResult
                {
                    ActivityMoment = x.CreatedMoment,
                    ActivityType = PreSalePurchaseActivity,
                    Reference = x.PreSaleOrderReference,
                    UserPublicKey = x.UserPublicKey,
                    WalletAddress = x.WalletAddress,
                    TokenSymbol = x.Symbol,
                    NativeAmount = x.ReceivingTokenAmount,
                    UsdValue = UsdValuationHelper.TokenValue(x.ReceivingTokenAmount, x.TokenPreSalePrice),
                    State = x.State.ToString(),
                    Hash = x.RegisterHash
                }));

            rows.AddRange(data.PreSaleOrders
                .Where(x => x.ReleaseSchedule != null && x.ReleaseSchedule.Any())
                .Where(x => identities.Any(i => Matches(i, x.UserPublicKey, x.WalletAddress)))
                .SelectMany(order => order.ReleaseSchedule.Select(step => ToConfirmedReleaseRow(order, step, data.ConfirmedReleaseLogs)))
                .Where(x => x != null));

            return rows;
        }

        private static UserReportHistoryResult ToConfirmedReleaseRow(
            PreSaleOrder order,
            PreSaleOrderReleaseStep step,
            List<TransactionLog> confirmedLogs)
        {
            var confirmedLog = FindConfirmedReleaseLog(order, step, confirmedLogs);
            if (confirmedLog == null)
                return null;

            return new UserReportHistoryResult
            {
                ActivityMoment = confirmedLog.CreatedMoment,
                ActivityType = ConfirmedReleaseActivity,
                Reference = order.PreSaleOrderReference,
                UserPublicKey = order.UserPublicKey,
                WalletAddress = order.WalletAddress,
                TokenSymbol = order.Symbol,
                NativeAmount = ReleaseAmount(order, step),
                UsdValue = UsdValuationHelper.TokenValue(ReleaseAmount(order, step), order.TokenPreSalePrice),
                State = "Confirmed",
                Hash = confirmedLog.Hash
            };
        }

        private static TransactionLog FindConfirmedReleaseLog(
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

        private static ReportTable ToTable(List<UserReportWalletResult> wallets, List<UserReportHistoryResult> history)
        {
            var summary = BuildSummary(wallets);
            var table = new ReportTable
            {
                Columns =
                [
                    new ReportTableColumn { Header = "Section", Width = 18 },
                    new ReportTableColumn { Header = "Date", Width = 20 },
                    new ReportTableColumn { Header = "Type", Width = 18 },
                    new ReportTableColumn { Header = "Reference", Width = 24 },
                    new ReportTableColumn { Header = "UserPublicKey", Width = 34 },
                    new ReportTableColumn { Header = "EVMWallet", Width = 34 },
                    new ReportTableColumn { Header = "TronWallet", Width = 34 },
                    new ReportTableColumn { Header = "Wallet", Width = 34 },
                    new ReportTableColumn { Header = "Token", Width = 16 },
                    new ReportTableColumn { Header = "NativeAmount", Width = 18 },
                    new ReportTableColumn { Header = "UsdValue", Width = 18 },
                    new ReportTableColumn { Header = "State", Width = 14 },
                    new ReportTableColumn { Header = "Hash", Width = 34 }
                ],
                Rows = []
            };

            table.Rows.Add(Row("Summary", string.Empty, "Users", summary.UserCount, string.Empty, summary.EvmWalletCount, summary.TronWalletCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", string.Empty, "Swaps", summary.SwapCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, summary.SwapUsdVolume, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", string.Empty, "Stakes", summary.StakeCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, summary.StakeUsdVolume, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", string.Empty, "PreSalePurchases", summary.PreSalePurchaseCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, summary.PreSaleSalesUsdValue, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", string.Empty, "ConfirmedReleases", summary.ConfirmedReleaseCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, summary.ConfirmedReleaseAmount, string.Empty, string.Empty, string.Empty));

            foreach (var wallet in wallets)
                table.Rows.Add(Row("Wallet", wallet.FirstActivityMoment.ToString("yyyy-MM-dd HH:mm:ss"), "FirstActivity", wallet.TotalActivityCount, wallet.UserPublicKey, wallet.EVMWalletAddress, wallet.TronWalletAddress, string.Empty, string.Empty, wallet.ConfirmedReleaseAmount, wallet.SwapUsdVolume + wallet.StakeUsdVolume + wallet.PreSaleSalesUsdValue, string.Empty, string.Empty));

            foreach (var row in history)
                table.Rows.Add(Row("History", row.ActivityMoment.ToString("yyyy-MM-dd HH:mm:ss"), row.ActivityType, row.Reference, row.UserPublicKey, string.Empty, string.Empty, row.WalletAddress, row.TokenSymbol, row.NativeAmount, row.UsdValue, row.State, row.Hash));

            return table;
        }

        private static bool MatchesFilter(UserIdentity identity, UserReportQueryUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.UserPublicKey) &&
                !Contains(identity.UserPublicKey, update.UserPublicKey.Trim()))
                return false;

            if (!string.IsNullOrWhiteSpace(update.Wallet) &&
                !Contains(identity.EVMWalletAddress, update.Wallet.Trim()) &&
                !Contains(identity.TronWalletAddress, update.Wallet.Trim()))
                return false;

            return true;
        }

        private static bool MatchesActivityFilter(UserReportHistoryResult row, UserReportQueryUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.UserPublicKey) &&
                !Contains(row.UserPublicKey, update.UserPublicKey.Trim()))
                return false;

            if (!string.IsNullOrWhiteSpace(update.Wallet) &&
                !Contains(row.WalletAddress, update.Wallet.Trim()))
                return false;

            return true;
        }

        private static bool IsInRange(DateTime moment, UserReportQueryUpdate update)
        {
            if (update.FromMoment != default && moment < update.FromMoment)
                return false;

            if (update.ToMoment != default && moment > update.ToMoment)
                return false;

            return true;
        }

        private static bool Matches(UserIdentity identity, string userPublicKey, string walletAddress)
        {
            if (!string.IsNullOrWhiteSpace(userPublicKey) &&
                EqualsText(identity.UserPublicKey, userPublicKey))
                return true;

            if (string.IsNullOrWhiteSpace(walletAddress))
                return false;

            return EqualsText(identity.EVMWalletAddress, walletAddress) ||
                   EqualsText(identity.TronWalletAddress, walletAddress);
        }

        private static void AddIdentityIfMissing(List<UserIdentity> identities, string userPublicKey, string walletAddress)
        {
            if (identities.Any(x => Matches(x, userPublicKey, walletAddress)))
                return;

            identities.Add(new UserIdentity
            {
                UserPublicKey = userPublicKey,
                EVMWalletAddress = walletAddress
            });
        }

        private static bool Contains(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);

        private static bool EqualsText(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               string.Equals(source.Trim(), value?.Trim(), StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<object> Row(params object[] values)
            => values;

        private class UserReportData
        {
            public List<User> Users { get; set; } = [];
            public List<Swap> Swaps { get; set; } = [];
            public List<Stake> Stakes { get; set; } = [];
            public List<PreSaleOrder> PreSaleOrders { get; set; } = [];
            public List<TransactionLog> ConfirmedReleaseLogs { get; set; } = [];
        }

        private class UserIdentity
        {
            public string UserPublicKey { get; set; }
            public string EVMWalletAddress { get; set; }
            public string TronWalletAddress { get; set; }
            public DateTime? UserCreatedMoment { get; set; }
        }
    }
}
