namespace CoinBank.Services._Report.Calculators
{
    public static class UsdValuationHelper
    {
        public static decimal TokenValue(decimal amount, decimal tokenPrice)
        {
            return amount * tokenPrice;
        }

        public static decimal PreSaleUsdtValue(decimal receivingTokenAmount, decimal tokenPreSalePrice)
        {
            return receivingTokenAmount * tokenPreSalePrice;
        }
    }
}
