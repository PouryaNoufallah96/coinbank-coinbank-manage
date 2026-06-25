namespace CoinBank.Services._Report.Calculators
{
    public static class PeriodBucketer
    {
        public static DateTime Day(DateTime moment)
        {
            return moment.Date;
        }

        public static DateTime Month(DateTime moment)
        {
            return new DateTime(moment.Year, moment.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static DateTime Week(DateTime moment)
        {
            var date = moment.Date;
            var offset = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-offset);
        }
    }
}
