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
    public interface IFinancialReportService
    {
        Task<FinancialReportResult> GetFinancialsAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportFinancialsExcelAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportFinancialsCsvAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportFinancialsPdfAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default);
    }

    public class FinancialReportService(
        IStakeRepository _stakeRepository,
        IPreSaleOrderRepository _preSaleOrderRepository,
        IWithdrawalRepository _withdrawalRepository,
        ITransactionLogRepository _transactionLogRepository,
        IReportExcelRenderer _excelRenderer,
        IReportCsvRenderer _csvRenderer,
        IReportPdfRenderer _pdfRenderer) : IFinancialReportService, IScopedDependency
    {
        private const string Inflow = "Inflow";
        private const string Outflow = "Outflow";
        private const string Commitment = "Commitment";
        private const string SubmittedUnconfirmed = "SubmittedUnconfirmed";
        private const string StakeInflow = "Stake";
        private const string PreSaleInflow = "PreSale";
        private const string WithdrawalOutflow = "Withdrawal";
        private const string PreSaleReleaseOutflow = "PreSaleRelease";
        private const string StakeRewardCommitment = "StakeReward";
        private const string LockedAssetCommitment = "LockedAsset";
        private const string SoldButUnreleasedCommitment = "SoldButUnreleasedPreSale";

        public async Task<FinancialReportResult> GetFinancialsAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            update ??= new FinancialReportQueryUpdate();
            ReportLimits.Enforce(update);

            var data = await LoadDataAsync(update, cancellationToken);
            var report = BuildReport(update, data);
            var page = update.Pagination;

            report.TransactionLogs = new ReportPageResult<FinancialTransactionLogResult>
            {
                TotalCount = data.TransactionLogs.Count,
                PageCount = (int)Math.Ceiling(data.TransactionLogs.Count / (double)page.Size),
                Data = data.TransactionLogs.Skip((page.Page - 1) * page.Size).Take(page.Size).Select(ToLogRow).ToList()
            };

            return report;
        }

        public async Task<ReportExportResult> ExportFinancialsExcelAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _excelRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportFinancialsCsvAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.csv",
                ContentType = "text/csv",
                Content = _csvRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportFinancialsPdfAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.pdf",
                ContentType = "application/pdf",
                Content = _pdfRenderer.Render(metadata, table)
            };
        }

        private async Task<(ExportMetadata Metadata, ReportTable Table)> BuildExportAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken)
        {
            update ??= new FinancialReportQueryUpdate();
            ReportLimits.Enforce(update);

            var data = await LoadDataAsync(update, cancellationToken);
            var report = BuildReport(update, data);
            report.TransactionLogs = new ReportPageResult<FinancialTransactionLogResult>
            {
                TotalCount = data.TransactionLogs.Count,
                PageCount = 1,
                Data = data.TransactionLogs.Select(ToLogRow).ToList()
            };

            var metadata = new ExportMetadata
            {
                Title = "Financial Report",
                FileName = $"financial-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Filters =
                [
                    new ExportFilterText { Label = "From", Value = update.FromMoment == default ? "All" : update.FromMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "To", Value = update.ToMoment == default ? "All" : update.ToMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "Wallet", Value = string.IsNullOrWhiteSpace(update.Wallet) ? "All" : update.Wallet },
                    new ExportFilterText { Label = "Token", Value = string.IsNullOrWhiteSpace(update.Token) ? "All" : update.Token },
                    new ExportFilterText { Label = "Source", Value = string.IsNullOrWhiteSpace(update.Source) ? "All" : update.Source },
                    new ExportFilterText { Label = "EventType", Value = update.EventType?.ToString() ?? "All" },
                    new ExportFilterText { Label = "TransactionStatus", Value = update.TransactionStatus?.ToString() ?? "All" }
                ]
            };

            var table = ToTable(report);
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            return (metadata, table);
        }

        private async Task<FinancialReportData> LoadDataAsync(FinancialReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var stakeQuery = _stakeRepository.AsQueryable();
            var preSaleOrderQuery = _preSaleOrderRepository.AsQueryable()
                .Where(x => x.State == PreSaleOrderState.InProgress ||
                            x.State == PreSaleOrderState.Completed);
            var releasePreSaleOrderQuery = _preSaleOrderRepository.AsQueryable()
                .Where(x => x.State == PreSaleOrderState.InProgress ||
                            x.State == PreSaleOrderState.Completed);
            var withdrawalQuery = _withdrawalRepository.AsQueryable()
                .Where(x => x.State == WithdrawalState.Success);
            var transactionLogQuery = _transactionLogRepository.AsQueryable();

            if (update.FromMoment != default)
            {
                stakeQuery = stakeQuery.Where(x => x.CreatedMoment >= update.FromMoment);
                preSaleOrderQuery = preSaleOrderQuery.Where(x => x.CreatedMoment >= update.FromMoment);
                withdrawalQuery = withdrawalQuery.Where(x => x.CreatedMoment >= update.FromMoment);
                transactionLogQuery = transactionLogQuery.Where(x => x.CreatedMoment >= update.FromMoment);
            }

            if (update.ToMoment != default)
            {
                stakeQuery = stakeQuery.Where(x => x.CreatedMoment <= update.ToMoment);
                preSaleOrderQuery = preSaleOrderQuery.Where(x => x.CreatedMoment <= update.ToMoment);
                withdrawalQuery = withdrawalQuery.Where(x => x.CreatedMoment <= update.ToMoment);
                transactionLogQuery = transactionLogQuery.Where(x => x.CreatedMoment <= update.ToMoment);
            }

            if (update.EventType.HasValue)
                transactionLogQuery = transactionLogQuery.Where(x => x.EventType == update.EventType.Value);

            if (update.TransactionStatus.HasValue)
                transactionLogQuery = transactionLogQuery.Where(x => x.Status == update.TransactionStatus.Value);

            var allStakes = await _stakeRepository.AsQueryable().ToListAsync(cancellationToken);
            var stakes = await stakeQuery.ToListAsync(cancellationToken);
            var preSaleOrders = await preSaleOrderQuery.ToListAsync(cancellationToken);
            var releasePreSaleOrders = await releasePreSaleOrderQuery.ToListAsync(cancellationToken);
            var withdrawals = await withdrawalQuery.ToListAsync(cancellationToken);
            var transactionLogs = await transactionLogQuery.ToListAsync(cancellationToken);

            stakes = FilterStakes(stakes, update);
            preSaleOrders = FilterPreSaleOrders(preSaleOrders, update);
            releasePreSaleOrders = FilterPreSaleOrders(releasePreSaleOrders, update);
            withdrawals = FilterWithdrawals(withdrawals, update);
            transactionLogs = FilterTransactionLogs(transactionLogs, update);

            var confirmedReleaseLogs = await _transactionLogRepository.AsQueryable()
                .Where(x => x.EventType == BlockchainEventType.PreSaleReleaseClaimed &&
                            x.Status == TransactionStatus.Confirmed)
                .ToListAsync(cancellationToken);

            return new FinancialReportData
            {
                Stakes = stakes,
                AllStakes = allStakes,
                PreSaleOrders = preSaleOrders,
                ReleasePreSaleOrders = releasePreSaleOrders,
                Withdrawals = withdrawals,
                TransactionLogs = transactionLogs
                    .OrderByDescending(x => x.CreatedMoment)
                    .ThenBy(x => x.TransactionLogId)
                    .ToList(),
                ConfirmedReleaseLogs = confirmedReleaseLogs
            };
        }

        private static FinancialReportResult BuildReport(FinancialReportQueryUpdate update, FinancialReportData data)
        {
            var stakeInflows = data.Stakes.Select(ToStakeInflow).ToList();
            var preSaleInflows = data.PreSaleOrders.Select(ToPreSaleInflow).ToList();
            var withdrawalOutflows = BuildWithdrawalOutflows(data);
            var commitmentReleaseRows = BuildReleaseRows(data.PreSaleOrders, data.ConfirmedReleaseLogs);
            var releaseRows = BuildReleaseRows(data.ReleasePreSaleOrders, data.ConfirmedReleaseLogs)
                .Where(x => IsInRange(x.CreatedMoment, update))
                .ToList();

            var confirmedReleaseOutflows = releaseRows.Where(x => x.CountsAsFinancialOutflow).ToList();
            var submittedUnconfirmedReleases = releaseRows.Where(x => !x.CountsAsFinancialOutflow && x.Source == SubmittedUnconfirmed).ToList();
            var inflows = FilterFlows(stakeInflows.Concat(preSaleInflows).ToList(), update);
            var outflows = FilterFlows(withdrawalOutflows.Concat(confirmedReleaseOutflows).ToList(), update);
            var submitted = FilterFlows(submittedUnconfirmedReleases, update);
            var commitments = FilterCommitments(BuildCommitments(
                data,
                commitmentReleaseRows.Where(x => x.CountsAsFinancialOutflow).ToList(),
                commitmentReleaseRows.Where(x => !x.CountsAsFinancialOutflow && x.Source == SubmittedUnconfirmed).ToList()), update);
            var allFlows = inflows.Concat(outflows).Concat(submitted).ToList();
            var summary = BuildSummary(commitments, inflows, outflows, submitted);

            return new FinancialReportResult
            {
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Summary = summary,
                Commitments = commitments,
                Inflows = SummarizeFlows(inflows),
                Outflows = SummarizeFlows(outflows.Concat(submitted).ToList()),
                NativeBreakdowns = BuildNativeBreakdowns(commitments, allFlows)
            };
        }

        private static FinancialFlowRow ToStakeInflow(Stake stake)
            => new()
            {
                Direction = Inflow,
                Source = StakeInflow,
                CreatedMoment = stake.CreatedMoment,
                Reference = stake.StakeReference,
                Wallet = stake.WalletAddress,
                TokenSymbol = stake.TokenSymbol,
                Network = stake.TokenNetworkName,
                NativeAmount = stake.StartAmount,
                UsdValue = UsdValuationHelper.TokenValue(stake.StartAmount, stake.TokenPrice),
                CountsAsFinancialOutflow = false
            };

        private static FinancialFlowRow ToPreSaleInflow(PreSaleOrder order)
            => new()
            {
                Direction = Inflow,
                Source = PreSaleInflow,
                CreatedMoment = order.CreatedMoment,
                Reference = order.PreSaleOrderReference,
                Wallet = order.WalletAddress,
                TokenSymbol = order.Symbol,
                Network = order.PaymentToken,
                NativeAmount = order.ReceivingTokenAmount,
                UsdValue = order.ReceivingTokenAmount * order.TokenPreSalePrice,
                CountsAsFinancialOutflow = false
            };

        private static List<FinancialFlowRow> BuildWithdrawalOutflows(FinancialReportData data)
        {
            var stakesByReference = data.AllStakes
                .Where(x => !string.IsNullOrWhiteSpace(x.StakeReference))
                .GroupBy(x => x.StakeReference.Trim().ToLowerInvariant())
                .ToDictionary(x => x.Key, x => x.First());

            return data.Withdrawals
                .Select(withdrawal =>
                {
                    stakesByReference.TryGetValue((withdrawal.StakeReference ?? string.Empty).Trim().ToLowerInvariant(), out var stake);

                    return new FinancialFlowRow
                    {
                        Direction = Outflow,
                        Source = WithdrawalOutflow,
                        CreatedMoment = withdrawal.CreatedMoment,
                        Reference = withdrawal.WithdrawalRerefence,
                        Wallet = withdrawal.WalletAddress,
                        TokenSymbol = withdrawal.Symbol,
                        Network = withdrawal.Network,
                        NativeAmount = withdrawal.FinalAmount,
                        UsdValue = UsdValuationHelper.TokenValue(withdrawal.FinalAmount, stake?.TokenPrice ?? 0),
                        CountsAsFinancialOutflow = true
                    };
                })
                .ToList();
        }

        private static List<FinancialFlowRow> BuildReleaseRows(List<PreSaleOrder> orders, List<TransactionLog> confirmedLogs)
        {
            return orders
                .Where(x => x.ReleaseSchedule != null)
                .SelectMany(order => order.ReleaseSchedule.Select(step => ToReleaseRow(order, step, confirmedLogs)))
                .Where(x => x != null)
                .ToList();
        }

        private static FinancialFlowRow ToReleaseRow(PreSaleOrder order, PreSaleOrderReleaseStep step, List<TransactionLog> confirmedLogs)
        {
            var confirmedLog = FindConfirmedReleaseLog(order, step, confirmedLogs);
            var confirmed = confirmedLog != null;
            var submitted = IsSubmittedRelease(step);

            if (!confirmed && !submitted)
                return null;

            var amount = ReleaseAmount(order, step);

            return new FinancialFlowRow
            {
                Direction = Outflow,
                Source = confirmed ? PreSaleReleaseOutflow : SubmittedUnconfirmed,
                CreatedMoment = confirmedLog?.CreatedMoment ?? step.RegisterMoment ?? step.ReleaseDate,
                Reference = order.PreSaleOrderReference,
                Wallet = order.WalletAddress,
                TokenSymbol = order.Symbol,
                Network = order.PaymentToken,
                NativeAmount = amount,
                UsdValue = amount * order.TokenPreSalePrice,
                CountsAsFinancialOutflow = confirmed
            };
        }

        private static TransactionLog FindConfirmedReleaseLog(PreSaleOrder order, PreSaleOrderReleaseStep step, List<TransactionLog> confirmedLogs)
        {
            if (string.IsNullOrWhiteSpace(step.RegisterHash))
                return null;

            return confirmedLogs.FirstOrDefault(x =>
                EqualsText(x.Hash, step.RegisterHash) &&
                EqualsText(x.Reference, order.PreSaleOrderReference));
        }

        private static List<FinancialCommitmentResult> BuildCommitments(
            FinancialReportData data,
            List<FinancialFlowRow> confirmedReleaseOutflows,
            List<FinancialFlowRow> submittedUnconfirmedReleases)
        {
            var soldButUnreleased = data.PreSaleOrders
                .GroupBy(x => new NativeKey(SoldButUnreleasedCommitment, x.Symbol, x.PaymentToken))
                .Select(group =>
                {
                    var sold = group.Sum(x => x.ReceivingTokenAmount);
                    var confirmed = confirmedReleaseOutflows
                        .Where(x => EqualsText(x.TokenSymbol, group.Key.TokenSymbol) && EqualsText(x.Network, group.Key.Network))
                        .Sum(x => x.NativeAmount);
                    var remaining = Math.Max(0, sold - confirmed);

                    return new FinancialCommitmentResult
                    {
                        CommitmentType = SoldButUnreleasedCommitment,
                        TokenSymbol = group.Key.TokenSymbol,
                        Network = group.Key.Network,
                        NativeAmount = sold,
                        UsdValue = group.Sum(x => x.ReceivingTokenAmount * x.TokenPreSalePrice),
                        PaidAmount = confirmed,
                        PaidUsdValue = confirmedReleaseOutflows
                            .Where(x => EqualsText(x.TokenSymbol, group.Key.TokenSymbol) && EqualsText(x.Network, group.Key.Network))
                            .Sum(x => x.UsdValue),
                        RemainingAmount = remaining,
                        RemainingUsdValue = Math.Max(0, group.Sum(x => x.ReceivingTokenAmount * x.TokenPreSalePrice) -
                                                        confirmedReleaseOutflows
                                                            .Where(x => EqualsText(x.TokenSymbol, group.Key.TokenSymbol) && EqualsText(x.Network, group.Key.Network))
                                                            .Sum(x => x.UsdValue))
                    };
                })
                .ToList();

            var rewards = data.Stakes
                .GroupBy(x => new NativeKey(StakeRewardCommitment, x.TokenSymbol, x.TokenNetworkName))
                .Select(group =>
                {
                    var committed = group.Sum(CommittedReward);
                    var paid = group.Sum(x => x.TotalProfitWithdrawn);
                    var remaining = group.Sum(RemainingReward);

                    return new FinancialCommitmentResult
                    {
                        CommitmentType = StakeRewardCommitment,
                        TokenSymbol = group.Key.TokenSymbol,
                        Network = group.Key.Network,
                        NativeAmount = committed,
                        UsdValue = group.Sum(x => UsdValuationHelper.TokenValue(CommittedReward(x), x.TokenPrice)),
                        PaidAmount = paid,
                        PaidUsdValue = group.Sum(x => UsdValuationHelper.TokenValue(x.TotalProfitWithdrawn, x.TokenPrice)),
                        RemainingAmount = remaining,
                        RemainingUsdValue = group.Sum(x => UsdValuationHelper.TokenValue(RemainingReward(x), x.TokenPrice))
                    };
                })
                .ToList();

            var lockedAssets = data.Stakes
                .Where(IsActive)
                .GroupBy(x => new NativeKey(LockedAssetCommitment, x.TokenSymbol, x.TokenNetworkName))
                .Select(group => new FinancialCommitmentResult
                {
                    CommitmentType = LockedAssetCommitment,
                    TokenSymbol = group.Key.TokenSymbol,
                    Network = group.Key.Network,
                    NativeAmount = group.Sum(x => x.TokenAmount),
                    UsdValue = group.Sum(x => UsdValuationHelper.TokenValue(x.TokenAmount, x.TokenPrice)),
                    RemainingAmount = group.Sum(x => x.TokenAmount),
                    RemainingUsdValue = group.Sum(x => UsdValuationHelper.TokenValue(x.TokenAmount, x.TokenPrice))
                })
                .ToList();

            return soldButUnreleased
                .Concat(rewards)
                .Concat(lockedAssets)
                .OrderBy(x => x.CommitmentType)
                .ThenBy(x => x.TokenSymbol)
                .ThenBy(x => x.Network)
                .ToList();
        }

        private static FinancialReportSummaryResult BuildSummary(
            List<FinancialCommitmentResult> commitments,
            List<FinancialFlowRow> inflows,
            List<FinancialFlowRow> outflows,
            List<FinancialFlowRow> submittedUnconfirmedReleases)
        {
            var soldButUnreleased = commitments.Where(x => x.CommitmentType == SoldButUnreleasedCommitment).ToList();
            var rewards = commitments.Where(x => x.CommitmentType == StakeRewardCommitment).ToList();
            var lockedAssets = commitments.Where(x => x.CommitmentType == LockedAssetCommitment).ToList();
            var totalInflow = inflows.Sum(x => x.UsdValue);
            var totalOutflow = outflows.Sum(x => x.UsdValue);

            return new FinancialReportSummaryResult
            {
                SoldButUnreleasedTokenAmount = soldButUnreleased.Sum(x => x.RemainingAmount),
                SoldButUnreleasedUsdValue = soldButUnreleased.Sum(x => x.RemainingUsdValue),
                ConfirmedReleasedTokenAmount = soldButUnreleased.Sum(x => x.PaidAmount),
                ConfirmedReleaseOutflowUsdValue = soldButUnreleased.Sum(x => x.PaidUsdValue),
                SubmittedUnconfirmedReleaseTokenAmount = submittedUnconfirmedReleases.Sum(x => x.NativeAmount),
                SubmittedUnconfirmedReleaseUsdValue = submittedUnconfirmedReleases.Sum(x => x.UsdValue),
                CommittedRewardAmount = rewards.Sum(x => x.NativeAmount),
                PaidRewardAmount = rewards.Sum(x => x.PaidAmount),
                RemainingRewardAmount = rewards.Sum(x => x.RemainingAmount),
                CommittedRewardUsdValue = rewards.Sum(x => x.UsdValue),
                PaidRewardUsdValue = rewards.Sum(x => x.PaidUsdValue),
                RemainingRewardUsdValue = rewards.Sum(x => x.RemainingUsdValue),
                LockedAssetAmount = lockedAssets.Sum(x => x.NativeAmount),
                LockedAssetUsdValue = lockedAssets.Sum(x => x.UsdValue),
                TotalInflowUsdValue = totalInflow,
                TotalOutflowUsdValue = totalOutflow,
                NetFlowUsdValue = totalInflow - totalOutflow
            };
        }

        private static List<FinancialFlowSummaryResult> SummarizeFlows(List<FinancialFlowRow> rows)
            => rows
                .GroupBy(x => new NativeKey(x.Source, x.TokenSymbol, x.Network))
                .Select(x => new FinancialFlowSummaryResult
                {
                    Direction = x.First().Direction,
                    Source = x.Key.Source,
                    TokenSymbol = x.Key.TokenSymbol,
                    Network = x.Key.Network,
                    Count = x.Count(),
                    NativeAmount = x.Sum(row => row.NativeAmount),
                    UsdValue = x.Sum(row => row.UsdValue),
                    CountsAsFinancialOutflow = x.Any(row => row.CountsAsFinancialOutflow)
                })
                .OrderBy(x => x.Direction)
                .ThenBy(x => x.Source)
                .ThenBy(x => x.TokenSymbol)
                .ThenBy(x => x.Network)
                .ToList();

        private static List<FinancialNativeBreakdownResult> BuildNativeBreakdowns(
            List<FinancialCommitmentResult> commitments,
            List<FinancialFlowRow> flows)
        {
            var commitmentRows = commitments.Select(x => new FinancialNativeBreakdownResult
            {
                Section = Commitment,
                Source = x.CommitmentType,
                TokenSymbol = x.TokenSymbol,
                Network = x.Network,
                NativeAmount = x.RemainingAmount == 0 ? x.NativeAmount : x.RemainingAmount,
                UsdValue = x.RemainingUsdValue == 0 ? x.UsdValue : x.RemainingUsdValue
            });

            var flowRows = flows
                .GroupBy(x => new NativeKey(x.Source, x.TokenSymbol, x.Network))
                .Select(x => new FinancialNativeBreakdownResult
                {
                    Section = x.First().Direction,
                    Source = x.Key.Source,
                    TokenSymbol = x.Key.TokenSymbol,
                    Network = x.Key.Network,
                    NativeAmount = x.Sum(row => row.NativeAmount),
                    UsdValue = x.Sum(row => row.UsdValue)
                });

            return commitmentRows
                .Concat(flowRows)
                .OrderBy(x => x.Section)
                .ThenBy(x => x.Source)
                .ThenBy(x => x.TokenSymbol)
                .ThenBy(x => x.Network)
                .ToList();
        }

        private static ReportTable ToTable(FinancialReportResult report)
        {
            var table = new ReportTable
            {
                Columns =
                [
                    new ReportTableColumn { Header = "Section", Width = 20 },
                    new ReportTableColumn { Header = "Source", Width = 22 },
                    new ReportTableColumn { Header = "Date", Width = 20 },
                    new ReportTableColumn { Header = "Reference", Width = 28 },
                    new ReportTableColumn { Header = "Wallet", Width = 34 },
                    new ReportTableColumn { Header = "Token", Width = 16 },
                    new ReportTableColumn { Header = "Network", Width = 16 },
                    new ReportTableColumn { Header = "NativeAmount", Width = 18 },
                    new ReportTableColumn { Header = "UsdValue", Width = 18 },
                    new ReportTableColumn { Header = "PaidNative", Width = 18 },
                    new ReportTableColumn { Header = "RemainingNative", Width = 18 },
                    new ReportTableColumn { Header = "EventType", Width = 22 },
                    new ReportTableColumn { Header = "Status", Width = 16 },
                    new ReportTableColumn { Header = "Hash", Width = 34 }
                ],
                Rows = []
            };

            table.Rows.Add(Row("Summary", "SoldButUnreleased", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, report.Summary.SoldButUnreleasedTokenAmount, report.Summary.SoldButUnreleasedUsdValue, report.Summary.ConfirmedReleasedTokenAmount, report.Summary.SoldButUnreleasedTokenAmount, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", "StakeRewards", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, report.Summary.CommittedRewardAmount, report.Summary.CommittedRewardUsdValue, report.Summary.PaidRewardAmount, report.Summary.RemainingRewardAmount, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", "LockedAssets", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, report.Summary.LockedAssetAmount, report.Summary.LockedAssetUsdValue, string.Empty, report.Summary.LockedAssetAmount, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", "TotalInflows", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, report.Summary.TotalInflowUsdValue, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", "TotalOutflows", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, report.Summary.TotalOutflowUsdValue, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
            table.Rows.Add(Row("Summary", "SubmittedUnconfirmedReleases", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, report.Summary.SubmittedUnconfirmedReleaseTokenAmount, report.Summary.SubmittedUnconfirmedReleaseUsdValue, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var commitment in report.Commitments)
                table.Rows.Add(Row(Commitment, commitment.CommitmentType, string.Empty, string.Empty, string.Empty, commitment.TokenSymbol, commitment.Network, commitment.NativeAmount, commitment.UsdValue, commitment.PaidAmount, commitment.RemainingAmount, string.Empty, string.Empty, string.Empty));

            foreach (var inflow in report.Inflows)
                table.Rows.Add(Row(Inflow, inflow.Source, string.Empty, string.Empty, string.Empty, inflow.TokenSymbol, inflow.Network, inflow.NativeAmount, inflow.UsdValue, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var outflow in report.Outflows)
                table.Rows.Add(Row(outflow.CountsAsFinancialOutflow ? Outflow : SubmittedUnconfirmed, outflow.Source, string.Empty, string.Empty, string.Empty, outflow.TokenSymbol, outflow.Network, outflow.NativeAmount, outflow.UsdValue, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var log in report.TransactionLogs.Data)
                table.Rows.Add(Row("TransactionLog", string.Empty, log.CreatedMoment.ToString("yyyy-MM-dd HH:mm:ss"), log.Reference, log.Wallet, string.Empty, log.Network, string.Empty, string.Empty, string.Empty, string.Empty, log.EventType, log.Status, log.Hash));

            return table;
        }

        private static FinancialTransactionLogResult ToLogRow(TransactionLog log)
            => new()
            {
                CreatedMoment = log.CreatedMoment,
                TransactionLogId = log.TransactionLogId,
                Reference = log.Reference,
                Wallet = log.Wallet,
                Hash = log.Hash,
                TokenAddress = log.TokenAddress,
                BlockNumber = log.BlockNumber,
                Network = log.Network,
                EventType = log.EventType,
                Status = log.Status
            };

        private static List<Stake> FilterStakes(List<Stake> stakes, FinancialReportQueryUpdate update)
        {
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

            return stakes;
        }

        private static List<PreSaleOrder> FilterPreSaleOrders(List<PreSaleOrder> orders, FinancialReportQueryUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.Wallet))
            {
                var wallet = update.Wallet.Trim();
                orders = orders
                    .Where(x => Contains(x.WalletAddress, wallet) ||
                                Contains(x.UserPublicKey, wallet))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                orders = orders
                    .Where(x => EqualsText(x.Symbol, token) ||
                                Contains(x.Name, token))
                    .ToList();
            }

            return orders;
        }

        private static List<Withdrawal> FilterWithdrawals(List<Withdrawal> withdrawals, FinancialReportQueryUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.Wallet))
            {
                var wallet = update.Wallet.Trim();
                withdrawals = withdrawals.Where(x => Contains(x.WalletAddress, wallet)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                withdrawals = withdrawals
                    .Where(x => EqualsText(x.Symbol, token) ||
                                EqualsText(x.Network, token))
                    .ToList();
            }

            return withdrawals;
        }

        private static List<TransactionLog> FilterTransactionLogs(List<TransactionLog> logs, FinancialReportQueryUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.Wallet))
            {
                var wallet = update.Wallet.Trim();
                logs = logs.Where(x => Contains(x.Wallet, wallet)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                logs = logs
                    .Where(x => EqualsText(x.Network, token) ||
                                Contains(x.TokenAddress, token))
                    .ToList();
            }

            return logs;
        }

        private static List<FinancialFlowRow> FilterFlows(List<FinancialFlowRow> rows, FinancialReportQueryUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.Source))
            {
                var source = update.Source.Trim();
                rows = rows.Where(x => EqualsText(x.Source, source) || EqualsText(x.Direction, source)).ToList();
            }

            return rows
                .OrderBy(x => x.Direction)
                .ThenBy(x => x.Source)
                .ThenBy(x => x.TokenSymbol)
                .ThenBy(x => x.Network)
                .ToList();
        }

        private static List<FinancialCommitmentResult> FilterCommitments(List<FinancialCommitmentResult> commitments, FinancialReportQueryUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.Source))
            {
                var source = update.Source.Trim();
                commitments = commitments
                    .Where(x => EqualsText(x.CommitmentType, source) || EqualsText(Commitment, source))
                    .ToList();
            }

            return commitments;
        }

        private static decimal ReleaseAmount(PreSaleOrder order, PreSaleOrderReleaseStep step)
        {
            if (step.CliamedAmount.HasValue && step.CliamedAmount.Value > 0)
                return step.CliamedAmount.Value;

            return order.ReceivingTokenAmount * step.Percentage / 100;
        }

        private static bool IsSubmittedRelease(PreSaleOrderReleaseStep step)
            => step.RegisterMoment.HasValue ||
               !string.IsNullOrWhiteSpace(step.RegisterHash) ||
               (step.CliamedAmount.HasValue && step.CliamedAmount.Value > 0);

        private static bool IsInRange(DateTime moment, FinancialReportQueryUpdate update)
        {
            if (update.FromMoment != default && moment < update.FromMoment)
                return false;

            if (update.ToMoment != default && moment > update.ToMoment)
                return false;

            return true;
        }

        private static bool IsActive(Stake stake)
            => stake.State == StakeState.Active;

        private static decimal CommittedReward(Stake stake)
            => stake.EachMonthProfit * stake.MonthDuration;

        private static decimal RemainingReward(Stake stake)
            => IsActive(stake) ? ObligationCalculator.RemainingStakeProfit(CommittedReward(stake), stake.TotalProfitWithdrawn) : 0;

        private static IReadOnlyList<object> Row(params object[] values)
            => values;

        private static bool Contains(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);

        private static bool EqualsText(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               string.Equals(source.Trim(), value, StringComparison.OrdinalIgnoreCase);

        private class FinancialReportData
        {
            public List<Stake> Stakes { get; set; } = [];
            public List<Stake> AllStakes { get; set; } = [];
            public List<PreSaleOrder> PreSaleOrders { get; set; } = [];
            public List<PreSaleOrder> ReleasePreSaleOrders { get; set; } = [];
            public List<Withdrawal> Withdrawals { get; set; } = [];
            public List<TransactionLog> TransactionLogs { get; set; } = [];
            public List<TransactionLog> ConfirmedReleaseLogs { get; set; } = [];
        }

        private class FinancialFlowRow
        {
            public string Direction { get; set; }
            public string Source { get; set; }
            public DateTime CreatedMoment { get; set; }
            public string Reference { get; set; }
            public string Wallet { get; set; }
            public string TokenSymbol { get; set; }
            public string Network { get; set; }
            public decimal NativeAmount { get; set; }
            public decimal UsdValue { get; set; }
            public bool CountsAsFinancialOutflow { get; set; }
        }

        private readonly record struct NativeKey(string Source, string TokenSymbol, string Network);
    }
}
