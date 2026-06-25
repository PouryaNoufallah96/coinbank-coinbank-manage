namespace CoinBank.Services._Report.Exports
{
    public interface IReportCsvRenderer
    {
        byte[] Render(ExportMetadata metadata, ReportTable table);
    }
}
