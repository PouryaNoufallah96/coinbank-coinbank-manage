namespace CoinBank.Services._Report.Exports
{
    public interface IReportPdfRenderer
    {
        byte[] Render(ExportMetadata metadata, ReportTable table);
    }
}
