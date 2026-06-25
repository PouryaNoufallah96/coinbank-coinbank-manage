namespace CoinBank.Services._Report.DTOs.Results
{
    public class PreSaleReportResult
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public PreSaleReportSummaryResult Summary { get; set; } = new();
        public List<PreSaleProjectReportResult> Projects { get; set; } = [];
        public ReportPageResult<PreSaleBuyerReportRowResult> Buyers { get; set; } = new();
        public List<PreSaleSalesVolumeResult> DailySales { get; set; } = [];
        public List<PreSaleSalesVolumeResult> MonthlySales { get; set; } = [];
        public List<PreSaleTokenRankingResult> TokenRanking { get; set; } = [];
    }

    public class PreSaleReportSummaryResult
    {
        public int OrderCount { get; set; }
        public int BuyerCount { get; set; }
        public decimal SoldTokenAmount { get; set; }
        public decimal SalesUsdValue { get; set; }
    }

    public class PreSaleProjectReportResult
    {
        public string PreSaleReference { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenName { get; set; }
        public decimal TokenPreSalePrice { get; set; }
        public decimal TotalSupply { get; set; }
        public decimal SoldTokenAmount { get; set; }
        public decimal RemainingTokenAmount { get; set; }
        public decimal SalesUsdValue { get; set; }
        public int BuyerCount { get; set; }
        public int OrderCount { get; set; }
        public DateTime StartSellingAt { get; set; }
        public DateTime EndSellingAt { get; set; }
        public string State { get; set; }
    }

    public class PreSaleBuyerReportRowResult
    {
        public DateTime CreatedMoment { get; set; }
        public string PreSaleOrderReference { get; set; }
        public string PreSaleReference { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenName { get; set; }
        public string WalletAddress { get; set; }
        public string UserPublicKey { get; set; }
        public decimal ReceivingTokenAmount { get; set; }
        public decimal TokenPreSalePrice { get; set; }
        public decimal SalesUsdValue { get; set; }
        public string PaymentToken { get; set; }
        public decimal PaymentTokenAmount { get; set; }
        public string State { get; set; }
        public string RegisterHash { get; set; }
        public DateTime? RegisterMoment { get; set; }
    }

    public class PreSaleSalesVolumeResult
    {
        public DateTime PeriodStart { get; set; }
        public decimal SoldTokenAmount { get; set; }
        public decimal SalesUsdValue { get; set; }
        public int OrderCount { get; set; }
        public int BuyerCount { get; set; }
    }

    public class PreSaleTokenRankingResult
    {
        public string PreSaleReference { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenName { get; set; }
        public decimal SoldTokenAmount { get; set; }
        public decimal SalesUsdValue { get; set; }
        public int BuyerCount { get; set; }
        public int OrderCount { get; set; }
    }
}
