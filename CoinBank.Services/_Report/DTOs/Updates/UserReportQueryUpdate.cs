namespace CoinBank.Services._Report.DTOs.Updates
{
    public class UserReportQueryUpdate : ReportQueryUpdate
    {
        public string Wallet { get; set; }
        public string UserPublicKey { get; set; }
    }
}
