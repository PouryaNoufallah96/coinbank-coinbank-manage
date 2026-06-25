namespace CoinBank.Services._Report.DTOs.Results
{
    public class TreasuryReportResult
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public TreasuryReportSummaryResult Summary { get; set; } = new();
        public ReportPageResult<TreasuryContractReportRowResult> Contracts { get; set; } = new();
        public List<TreasuryMaturityWindowResult> MaturityWindows { get; set; } = [];
        public List<TreasuryMaturityScheduleResult> FutureMaturitySchedule { get; set; } = [];
    }

    public class TreasuryReportSummaryResult
    {
        public int ActiveUserCount { get; set; }
        public int ActiveContractCount { get; set; }
        public int TotalContractCount { get; set; }
        public decimal TotalHistoricalUsdVolume { get; set; }
        public decimal TotalLockedUsdValue { get; set; }
        public decimal CommittedReward { get; set; }
        public decimal PaidReward { get; set; }
        public decimal RemainingReward { get; set; }
        public decimal CommittedRewardUsdValue { get; set; }
        public decimal PaidRewardUsdValue { get; set; }
        public decimal RemainingRewardUsdValue { get; set; }
    }

    public class TreasuryContractReportRowResult
    {
        public DateTime CreatedMoment { get; set; }
        public string StakeReference { get; set; }
        public string WalletAddress { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenName { get; set; }
        public string TokenNetworkName { get; set; }
        public decimal StartAmount { get; set; }
        public decimal TokenAmount { get; set; }
        public decimal TokenPrice { get; set; }
        public decimal HistoricalUsdVolume { get; set; }
        public decimal LockedUsdValue { get; set; }
        public decimal EachMonthProfit { get; set; }
        public int MonthDuration { get; set; }
        public decimal CommittedReward { get; set; }
        public decimal PaidReward { get; set; }
        public decimal RemainingReward { get; set; }
        public decimal RemainingRewardUsdValue { get; set; }
        public decimal PrincipalDue { get; set; }
        public decimal PrincipalDueUsdValue { get; set; }
        public decimal PrincipalAndRewardDueUsdValue { get; set; }
        public DateTime StartMoment { get; set; }
        public DateTime EndMoment { get; set; }
        public string State { get; set; }
        public string RegisterHash { get; set; }
        public DateTime? RegisterMoment { get; set; }
    }

    public class TreasuryMaturityWindowResult
    {
        public string Window { get; set; }
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public int ContractCount { get; set; }
        public decimal PrincipalDueUsdValue { get; set; }
        public decimal RewardDueUsdValue { get; set; }
        public decimal PrincipalAndRewardDueUsdValue { get; set; }
    }

    public class TreasuryMaturityScheduleResult
    {
        public DateTime PeriodStart { get; set; }
        public int ContractCount { get; set; }
        public decimal PrincipalDueUsdValue { get; set; }
        public decimal RewardDueUsdValue { get; set; }
        public decimal PrincipalAndRewardDueUsdValue { get; set; }
    }
}
