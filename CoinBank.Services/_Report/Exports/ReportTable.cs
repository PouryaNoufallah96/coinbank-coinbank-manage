namespace CoinBank.Services._Report.Exports
{
    public class ReportTable
    {
        public List<ReportTableColumn> Columns { get; set; } = [];
        public List<IReadOnlyList<object>> Rows { get; set; } = [];
    }

    public class ReportTableColumn
    {
        public string Header { get; set; }
        public double Width { get; set; } = 18;
    }
}
