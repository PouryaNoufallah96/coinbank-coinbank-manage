namespace CoinBank.Services._Report.DTOs.Results
{
    public class ReportExportResult
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }
}
