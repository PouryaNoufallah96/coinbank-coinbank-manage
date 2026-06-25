namespace CoinBank.Services._Report.Exports
{
    public interface IReportExcelRenderer
    {
        byte[] Render(ExportMetadata metadata, ReportTable table);
    }
}
