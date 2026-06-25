namespace CoinBank.Services._Report.DTOs.Results
{
    public class ReportPageResult<T>
    {
        public int TotalCount { get; set; }
        public int PageCount { get; set; }
        public List<T> Data { get; set; } = [];
    }
}
