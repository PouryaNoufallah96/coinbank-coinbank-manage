using CoinBank.Domain.Collections;

namespace CoinBank.Services._Report.DTOs.Results
{
    public class FinancialReportResult
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public FinancialReportSummaryResult Summary { get; set; } = new();
        public List<FinancialCommitmentResult> Commitments { get; set; } = [];
        public List<FinancialFlowSummaryResult> Inflows { get; set; } = [];
        public List<FinancialFlowSummaryResult> Outflows { get; set; } = [];
        public List<FinancialNativeBreakdownResult> NativeBreakdowns { get; set; } = [];
        public ReportPageResult<FinancialTransactionLogResult> TransactionLogs { get; set; } = new();
    }

    public class FinancialReportSummaryResult
    {
        public decimal SoldButUnreleasedTokenAmount { get; set; }
        public decimal SoldButUnreleasedUsdValue { get; set; }
        public decimal ConfirmedReleasedTokenAmount { get; set; }
        public decimal ConfirmedReleaseOutflowUsdValue { get; set; }
        public decimal SubmittedUnconfirmedReleaseTokenAmount { get; set; }
        public decimal SubmittedUnconfirmedReleaseUsdValue { get; set; }
        public decimal CommittedRewardAmount { get; set; }
        public decimal PaidRewardAmount { get; set; }
        public decimal RemainingRewardAmount { get; set; }
        public decimal CommittedRewardUsdValue { get; set; }
        public decimal PaidRewardUsdValue { get; set; }
        public decimal RemainingRewardUsdValue { get; set; }
        public decimal LockedAssetAmount { get; set; }
        public decimal LockedAssetUsdValue { get; set; }
        public decimal TotalInflowUsdValue { get; set; }
        public decimal TotalOutflowUsdValue { get; set; }
        public decimal NetFlowUsdValue { get; set; }
    }

    public class FinancialCommitmentResult
    {
        public string CommitmentType { get; set; }
        public string TokenSymbol { get; set; }
        public string Network { get; set; }
        public decimal NativeAmount { get; set; }
        public decimal UsdValue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PaidUsdValue { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal RemainingUsdValue { get; set; }
    }

    public class FinancialFlowSummaryResult
    {
        public string Direction { get; set; }
        public string Source { get; set; }
        public string TokenSymbol { get; set; }
        public string Network { get; set; }
        public int Count { get; set; }
        public decimal NativeAmount { get; set; }
        public decimal UsdValue { get; set; }
        public bool CountsAsFinancialOutflow { get; set; }
    }

    public class FinancialNativeBreakdownResult
    {
        public string Section { get; set; }
        public string Source { get; set; }
        public string TokenSymbol { get; set; }
        public string Network { get; set; }
        public decimal NativeAmount { get; set; }
        public decimal UsdValue { get; set; }
    }

    public class FinancialTransactionLogResult
    {
        public DateTime CreatedMoment { get; set; }
        public string TransactionLogId { get; set; }
        public string Reference { get; set; }
        public string Wallet { get; set; }
        public string Hash { get; set; }
        public string TokenAddress { get; set; }
        public decimal BlockNumber { get; set; }
        public string Network { get; set; }
        public BlockchainEventType EventType { get; set; }
        public TransactionStatus Status { get; set; }
    }
}
