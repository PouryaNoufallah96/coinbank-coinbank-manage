using Utilities.DTOs;

namespace CoinBank.Services._Report.DTOs.Updates
{
    public class ReportQueryUpdate
    {
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public Pagination Pagination { get; set; } = new();
    }
}
