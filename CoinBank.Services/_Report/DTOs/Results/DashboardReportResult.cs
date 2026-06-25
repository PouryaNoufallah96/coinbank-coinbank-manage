namespace CoinBank.Services._Report.DTOs.Results
{
    public class DashboardReportResult
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public int UniqueUsers { get; set; }
        public decimal SwapUsdVolume { get; set; }
        public decimal TreasuryProgramUsdVolume { get; set; }
        public decimal PreSaleUsdVolume { get; set; }
        public decimal LockedAssetsUsdValue { get; set; }
        public decimal TokensSold { get; set; }
        public int ActivePreSales { get; set; }
        public int ActiveTreasuryPrograms { get; set; }
        public int ActiveTreasuryContracts { get; set; }
        public List<DashboardTransactionCountResult> TransactionCounts { get; set; } = [];
        public List<DashboardActivityPointResult> DailyActivity { get; set; } = [];
        public List<DashboardActivityPointResult> WeeklyActivity { get; set; } = [];
        public List<DashboardActivityPointResult> MonthlyActivity { get; set; } = [];
    }

    public class DashboardTransactionCountResult
    {
        public string Type { get; set; }
        public string State { get; set; }
        public int Count { get; set; }
    }

    public class DashboardActivityPointResult
    {
        public DateTime PeriodStart { get; set; }
        public int Users { get; set; }
        public int Swaps { get; set; }
        public int TreasuryPrograms { get; set; }
        public int PreSaleOrders { get; set; }
        public int Withdrawals { get; set; }
    }
}
