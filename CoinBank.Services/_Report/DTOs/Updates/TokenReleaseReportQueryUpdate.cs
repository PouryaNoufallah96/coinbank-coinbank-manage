namespace CoinBank.Services._Report.DTOs.Updates
{
    public class TokenReleaseReportQueryUpdate : ReportQueryUpdate
    {
        public string Wallet { get; set; }
        public string Token { get; set; }
        public string Status { get; set; }
    }
}
