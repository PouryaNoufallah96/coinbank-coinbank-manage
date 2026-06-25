namespace CoinBank.Services._Report.Exports
{
    public class ExportMetadata
    {
        public string Title { get; set; }
        public string FileName { get; set; }
        public DateTime FromMoment { get; set; }
        public DateTime ToMoment { get; set; }
        public DateTime GeneratedMoment { get; set; } = DateTime.UtcNow;
        public List<ExportFilterText> Filters { get; set; } = [];
    }
}
