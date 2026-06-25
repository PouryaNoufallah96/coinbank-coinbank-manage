namespace CoinBank.Services._Report.Calculators
{
    public static class MaturityCalculator
    {
        public static bool IsMatured(DateTime maturityMoment, DateTime now)
        {
            return maturityMoment <= now;
        }

        public static int DaysUntil(DateTime maturityMoment, DateTime now)
        {
            return Math.Max(0, (int)Math.Ceiling((maturityMoment.Date - now.Date).TotalDays));
        }
    }
}
