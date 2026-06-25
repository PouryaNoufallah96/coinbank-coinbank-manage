using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Services._Report.Exports
{
    public class ReportPdfRenderer : IReportPdfRenderer, IScopedDependency
    {
        private static bool _fontResolverRegistered;

        public byte[] Render(ExportMetadata metadata, ReportTable table)
        {
            ReportLimits.EnforceExportRowCap(table.Rows.Count);
            EnsureFontResolver();

            var document = new Document();
            document.Info.Title = metadata.Title;
            document.Styles["Normal"].Font.Name = "Roboto";
            document.Styles["Normal"].Font.Size = 8;

            var section = document.AddSection();
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1);

            var title = section.AddParagraph(metadata.Title);
            title.Format.Font.Size = 14;
            title.Format.Font.Bold = true;
            section.AddParagraph($"Generated: {metadata.GeneratedMoment:yyyy-MM-dd HH:mm:ss} UTC");
            section.AddParagraph($"Range: {metadata.FromMoment:yyyy-MM-dd} - {metadata.ToMoment:yyyy-MM-dd}");
            section.AddParagraph();

            var tableDocument = section.AddTable();
            tableDocument.Borders.Width = 0.25;

            foreach (var column in table.Columns)
                tableDocument.AddColumn(Unit.FromCentimeter(Math.Max(1, column.Width / 4)));

            var header = tableDocument.AddRow();
            header.Shading.Color = Colors.LightGray;
            header.Format.Font.Bold = true;
            for (var i = 0; i < table.Columns.Count; i++)
                header.Cells[i].AddParagraph(table.Columns[i].Header);

            foreach (var sourceRow in table.Rows)
            {
                var row = tableDocument.AddRow();
                for (var i = 0; i < sourceRow.Count && i < table.Columns.Count; i++)
                    row.Cells[i].AddParagraph(sourceRow[i]?.ToString() ?? string.Empty);
            }

            var renderer = new PdfDocumentRenderer
            {
                Document = document
            };
            renderer.RenderDocument();

            using var stream = new MemoryStream();
            renderer.PdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private static void EnsureFontResolver()
        {
            if (_fontResolverRegistered)
                return;

            GlobalFontSettings.FontResolver ??= new ReportPdfFontResolver();
            _fontResolverRegistered = true;
        }
    }
}
