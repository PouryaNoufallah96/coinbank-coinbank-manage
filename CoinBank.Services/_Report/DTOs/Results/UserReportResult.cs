namespace CoinBank.Services._Report.DTOs.Results
{
    public class UserReportResult
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public UserReportSummaryResult Summary { get; set; } = new();
        public ReportPageResult<UserReportWalletResult> Wallets { get; set; } = new();
        public ReportPageResult<UserReportHistoryResult> History { get; set; } = new();
    }

    public class UserReportSummaryResult
    {
        public int UserCount { get; set; }
        public int EvmWalletCount { get; set; }
        public int TronWalletCount { get; set; }
        public int SwapCount { get; set; }
        public decimal SwapUsdVolume { get; set; }
        public int StakeCount { get; set; }
        public decimal StakeUsdVolume { get; set; }
        public decimal CurrentLockedUsdValue { get; set; }
        public int PreSalePurchaseCount { get; set; }
        public decimal PreSaleSalesUsdValue { get; set; }
        public int ConfirmedReleaseCount { get; set; }
        public decimal ConfirmedReleaseAmount { get; set; }
    }

    public class UserReportWalletResult
    {
        public string UserPublicKey { get; set; }
        public string EVMWalletAddress { get; set; }
        public string TronWalletAddress { get; set; }
        public DateTime FirstActivityMoment { get; set; }
        public int SwapCount { get; set; }
        public decimal SwapUsdVolume { get; set; }
        public int StakeCount { get; set; }
        public decimal StakeUsdVolume { get; set; }
        public decimal CurrentLockedUsdValue { get; set; }
        public int PreSalePurchaseCount { get; set; }
        public decimal PreSaleSalesUsdValue { get; set; }
        public int ConfirmedReleaseCount { get; set; }
        public decimal ConfirmedReleaseAmount { get; set; }
        public int TotalActivityCount { get; set; }
    }

    public class UserReportHistoryResult
    {
        public DateTime ActivityMoment { get; set; }
        public string ActivityType { get; set; }
        public string Reference { get; set; }
        public string UserPublicKey { get; set; }
        public string WalletAddress { get; set; }
        public string TokenSymbol { get; set; }
        public decimal NativeAmount { get; set; }
        public decimal? UsdValue { get; set; }
        public decimal? LockedUsdValue { get; set; }
        public string State { get; set; }
        public string Hash { get; set; }
    }
}
