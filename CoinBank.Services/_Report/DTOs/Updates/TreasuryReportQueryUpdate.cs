using CoinBank.Domain.Collections;

namespace CoinBank.Services._Report.DTOs.Updates
{
    public class TreasuryReportQueryUpdate : ReportQueryUpdate
    {
        public string Wallet { get; set; }
        public string Token { get; set; }
        public StakeState? Status { get; set; }
    }
}
