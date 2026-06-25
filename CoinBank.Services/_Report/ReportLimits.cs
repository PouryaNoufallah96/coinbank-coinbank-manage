using CoinBank.Services._Report.DTOs.Updates;
using Utilities.Exceptions.Common;

namespace CoinBank.Services._Report
{
    public static class ReportLimits
    {
        public const int MaxDateRangeDays = 366;
        public const int MaxPageSize = 100;
        public const int ExportRowCap = 10000;

        public static void Enforce(ReportQueryUpdate update, bool requireDateRange = false)
        {
            if (update == null)
                throw new BadRequestException("Report filter is required");

            var hasFrom = update.FromMoment != default;
            var hasTo = update.ToMoment != default;

            if (requireDateRange && (!hasFrom || !hasTo))
                throw new BadRequestException("Report date range is required");

            if (hasFrom != hasTo)
                throw new BadRequestException("Report date range must include both start and end dates");

            if (hasFrom && update.FromMoment > update.ToMoment)
                throw new BadRequestException("Report start date cannot be after end date");

            if (hasFrom && (update.ToMoment - update.FromMoment).TotalDays > MaxDateRangeDays)
                throw new BadRequestException($"Report date range cannot exceed {MaxDateRangeDays} days");

            update.Pagination ??= new();
            if (update.Pagination.Page < 1)
                update.Pagination.Page = 1;

            update.Pagination.Size = Math.Min(Math.Max(update.Pagination.Size, 1), MaxPageSize);
        }

        public static void EnforceExportRowCap(int rowCount)
        {
            if (rowCount > ExportRowCap)
                throw new BadRequestException($"Report export cannot exceed {ExportRowCap} rows");
        }
    }
}
