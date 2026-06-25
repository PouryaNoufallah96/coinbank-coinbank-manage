namespace CoinBank.Services._Report.DTOs.Results
{
    public class TokenReleaseReportResult
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public TokenReleaseReportSummaryResult Summary { get; set; } = new();
        public ReportPageResult<TokenReleaseReportRowResult> Releases { get; set; } = new();
        public List<TokenReleaseWindowResult> ReleaseWindows { get; set; } = [];
        public List<TokenReleaseCalendarResult> ReleaseCalendar { get; set; } = [];
    }

    public class TokenReleaseReportSummaryResult
    {
        public int ReleaseCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int ScheduledCount { get; set; }
        public decimal ScheduledAmount { get; set; }
        public int SubmittedCount { get; set; }
        public decimal SubmittedAmount { get; set; }
        public int ConfirmedCount { get; set; }
        public decimal ConfirmedAmount { get; set; }
    }

    public class TokenReleaseReportRowResult
    {
        public DateTime CreatedMoment { get; set; }
        public string PreSaleOrderReference { get; set; }
        public string PreSaleReference { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenName { get; set; }
        public string WalletAddress { get; set; }
        public string UserPublicKey { get; set; }
        public decimal Percentage { get; set; }
        public decimal Amount { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Status { get; set; }
        public string RegisterHash { get; set; }
        public DateTime? RegisterMoment { get; set; }
        public string ConfirmedHash { get; set; }
        public DateTime? ConfirmedMoment { get; set; }
    }

    public class TokenReleaseWindowResult
    {
        public string Window { get; set; }
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public int ReleaseCount { get; set; }
        public decimal Amount { get; set; }
        public int ScheduledCount { get; set; }
        public decimal ScheduledAmount { get; set; }
        public int SubmittedCount { get; set; }
        public decimal SubmittedAmount { get; set; }
        public int ConfirmedCount { get; set; }
        public decimal ConfirmedAmount { get; set; }
    }

    public class TokenReleaseCalendarResult
    {
        public DateTime PeriodStart { get; set; }
        public int ReleaseCount { get; set; }
        public decimal Amount { get; set; }
        public int ScheduledCount { get; set; }
        public decimal ScheduledAmount { get; set; }
        public int SubmittedCount { get; set; }
        public decimal SubmittedAmount { get; set; }
        public int ConfirmedCount { get; set; }
        public decimal ConfirmedAmount { get; set; }
    }
}
