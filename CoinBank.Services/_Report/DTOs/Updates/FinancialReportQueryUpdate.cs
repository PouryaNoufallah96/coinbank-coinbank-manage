using CoinBank.Domain.Collections;

namespace CoinBank.Services._Report.DTOs.Updates
{
    public class FinancialReportQueryUpdate : ReportQueryUpdate
    {
        public string Wallet { get; set; }
        public string Token { get; set; }
        public string Source { get; set; }
        public BlockchainEventType? EventType { get; set; }
        public TransactionStatus? TransactionStatus { get; set; }
    }
}
