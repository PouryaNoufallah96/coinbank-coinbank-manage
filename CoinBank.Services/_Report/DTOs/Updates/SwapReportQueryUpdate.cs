namespace CoinBank.Services._Report.DTOs.Updates
{
    public class SwapReportQueryUpdate : ReportQueryUpdate
    {
        public string Wallet { get; set; }
        public string Hash { get; set; }
        public string Token { get; set; }
        public string Network { get; set; }
    }
}
