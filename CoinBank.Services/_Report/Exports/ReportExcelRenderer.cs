using ClosedXML.Excel;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Services._Report.Exports
{
    public class ReportExcelRenderer : IReportExcelRenderer, IScopedDependency
    {
        public byte[] Render(ExportMetadata metadata, ReportTable table)
        {
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report");

            worksheet.Cell(1, 1).Value = metadata.Title;
            worksheet.Cell(2, 1).Value = $"Generated: {metadata.GeneratedMoment:yyyy-MM-dd HH:mm:ss} UTC";
            worksheet.Cell(3, 1).Value = $"Range: {metadata.FromMoment:yyyy-MM-dd} - {metadata.ToMoment:yyyy-MM-dd}";

            var headerRow = 5;
            for (var i = 0; i < table.Columns.Count; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = table.Columns[i].Header;
                worksheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
            }

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
                    worksheet.Cell(headerRow + rowIndex + 1, columnIndex + 1).Value = XLCellValue.FromObject(row[columnIndex]);
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
