namespace CoinBank.Services._Report.DTOs.Results
{
    public class SwapReportResult
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public ReportPageResult<SwapReportRowResult> Swaps { get; set; } = new();
        public List<SwapVolumePointResult> DailyUsdVolume { get; set; } = [];
        public List<SwapVolumePointResult> MonthlyUsdVolume { get; set; } = [];
        public List<SwapFeeRevenueResult> FeeRevenue { get; set; } = [];
        public List<SwapTopUserResult> TopUsersByVolume { get; set; } = [];
        public List<SwapTokenVolumeResult> MostTradedTokens { get; set; } = [];
    }

    public class SwapReportRowResult
    {
        public DateTime CreatedMoment { get; set; }
        public string SwapReference { get; set; }
        public string UserPublicKey { get; set; }
        public string WalletAddress { get; set; }
        public string SourceNetwork { get; set; }
        public string SourceSymbol { get; set; }
        public decimal SourceAmount { get; set; }
        public decimal SourceTokenPrice { get; set; }
        public decimal UsdVolume { get; set; }
        public string DestinationNetwork { get; set; }
        public string DestinationSymbol { get; set; }
        public decimal DestinationAmount { get; set; }
        public decimal DestinationTokenPrice { get; set; }
        public string FeeToken { get; set; }
        public decimal Fee { get; set; }
        public decimal? FeeUsdValue { get; set; }
        public string RegisterHash { get; set; }
        public DateTime? RegisterMoment { get; set; }
        public string RefundHash { get; set; }
        public DateTime? RefundMoment { get; set; }
        public string State { get; set; }
    }

    public class SwapVolumePointResult
    {
        public DateTime PeriodStart { get; set; }
        public decimal UsdVolume { get; set; }
        public int SwapCount { get; set; }
    }

    public class SwapFeeRevenueResult
    {
        public string FeeToken { get; set; }
        public decimal NativeAmount { get; set; }
        public decimal? UsdValue { get; set; }
        public int SwapCount { get; set; }
        public int UsdDerivableCount { get; set; }
    }

    public class SwapTopUserResult
    {
        public string WalletAddress { get; set; }
        public string UserPublicKey { get; set; }
        public decimal UsdVolume { get; set; }
        public int SwapCount { get; set; }
    }

    public class SwapTokenVolumeResult
    {
        public string Token { get; set; }
        public decimal NativeVolume { get; set; }
        public decimal UsdVolume { get; set; }
        public int SwapCount { get; set; }
    }
}
