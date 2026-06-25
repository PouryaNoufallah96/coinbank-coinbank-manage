namespace CoinBank.Services._Report.Calculators
{
    public static class ObligationCalculator
    {
        public static decimal RemainingStakePrincipal(decimal startAmount, decimal totalAmountWithdrawn)
        {
            return Math.Max(0, startAmount - totalAmountWithdrawn);
        }

        public static decimal RemainingStakeProfit(decimal expectedProfit, decimal totalProfitWithdrawn)
        {
            return Math.Max(0, expectedProfit - totalProfitWithdrawn);
        }

        public static decimal RemainingReleaseAmount(decimal receivingAmount, decimal claimedAmount)
        {
            return Math.Max(0, receivingAmount - claimedAmount);
        }
    }
}
