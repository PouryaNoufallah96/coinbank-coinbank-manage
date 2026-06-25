using System.Globalization;
using System.Text;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Services._Report.Exports
{
    public class ReportCsvRenderer : IReportCsvRenderer, IScopedDependency
    {
        public byte[] Render(ExportMetadata metadata, ReportTable table)
        {
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", table.Columns.Select(c => Escape(c.Header))));

            foreach (var row in table.Rows)
                builder.AppendLine(string.Join(",", row.Select(Escape)));

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        private static string Escape(object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (!text.Contains(',') && !text.Contains('"') && !text.Contains('\n') && !text.Contains('\r'))
                return text;

            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
    }
}
