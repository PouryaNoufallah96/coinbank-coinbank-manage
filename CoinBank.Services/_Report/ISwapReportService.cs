using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using CoinBank.Services._Report.Calculators;
using CoinBank.Services._Report.DTOs.Results;
using CoinBank.Services._Report.DTOs.Updates;
using CoinBank.Services._Report.Exports;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Services._Report
{
    public interface ISwapReportService
    {
        Task<SwapReportResult> GetSwapsAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportSwapsExcelAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportSwapsCsvAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportSwapsPdfAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default);
    }

    public class SwapReportService(
        ISwapRepository _swapRepository,
        IReportExcelRenderer _excelRenderer,
        IReportCsvRenderer _csvRenderer,
        IReportPdfRenderer _pdfRenderer) : ISwapReportService, IScopedDependency
    {
        public async Task<SwapReportResult> GetSwapsAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            update ??= new SwapReportQueryUpdate();
            ReportLimits.Enforce(update);

            var swaps = await GetFilteredSwapsAsync(update, cancellationToken);
            var rows = swaps.Select(ToRow).ToList();
            var page = update.Pagination;

            return new SwapReportResult
            {
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Swaps = new ReportPageResult<SwapReportRowResult>
                {
                    TotalCount = rows.Count,
                    PageCount = (int)Math.Ceiling(rows.Count / (double)page.Size),
                    Data = rows.Skip((page.Page - 1) * page.Size).Take(page.Size).ToList()
                },
                DailyUsdVolume = BuildVolume(swaps, PeriodBucketer.Day),
                MonthlyUsdVolume = BuildVolume(swaps, PeriodBucketer.Month),
                FeeRevenue = BuildFeeRevenue(swaps),
                TopUsersByVolume = BuildTopUsers(swaps),
                MostTradedTokens = BuildMostTradedTokens(swaps)
            };
        }

        public async Task<ReportExportResult> ExportSwapsExcelAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _excelRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportSwapsCsvAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.csv",
                ContentType = "text/csv",
                Content = _csvRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportSwapsPdfAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.pdf",
                ContentType = "application/pdf",
                Content = _pdfRenderer.Render(metadata, table)
            };
        }

        private async Task<(ExportMetadata Metadata, ReportTable Table)> BuildExportAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken)
        {
            update ??= new SwapReportQueryUpdate();
            ReportLimits.Enforce(update);

            var swaps = await GetFilteredSwapsAsync(update, cancellationToken);

            var metadata = new ExportMetadata
            {
                Title = "Swap Report",
                FileName = $"swap-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Filters =
                [
                    new ExportFilterText { Label = "From", Value = update.FromMoment == default ? "All" : update.FromMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "To", Value = update.ToMoment == default ? "All" : update.ToMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "Wallet", Value = string.IsNullOrWhiteSpace(update.Wallet) ? "All" : update.Wallet },
                    new ExportFilterText { Label = "Hash", Value = string.IsNullOrWhiteSpace(update.Hash) ? "All" : update.Hash },
                    new ExportFilterText { Label = "Token", Value = string.IsNullOrWhiteSpace(update.Token) ? "All" : update.Token },
                    new ExportFilterText { Label = "Network", Value = string.IsNullOrWhiteSpace(update.Network) ? "All" : update.Network }
                ]
            };

            var table = ToTable(swaps);
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            return (metadata, table);
        }

        private async Task<List<Swap>> GetFilteredSwapsAsync(SwapReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var query = _swapRepository.AsQueryable();

            if (update.FromMoment != default)
                query = query.Where(x => x.CreatedMoment >= update.FromMoment);

            if (update.ToMoment != default)
                query = query.Where(x => x.CreatedMoment <= update.ToMoment);

            var swaps = await query.ToListAsync();

            if (!string.IsNullOrWhiteSpace(update.Wallet))
            {
                var wallet = update.Wallet.Trim();
                swaps = swaps
                    .Where(x => Contains(x.WalletAddress, wallet) ||
                                Contains(x.UserPublicKey, wallet) ||
                                Contains(x.SourceWallet, wallet) ||
                                Contains(x.DestinationWallet, wallet))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Hash))
            {
                var hash = update.Hash.Trim();
                swaps = swaps
                    .Where(x => Contains(x.RegisterHash, hash) ||
                                Contains(x.SwapRefundRegisterHash, hash) ||
                                x.Transactions.Any(t => Contains(t.Hash, hash)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                swaps = swaps
                    .Where(x => EqualsText(x.SourceSymbol, token) ||
                                EqualsText(x.DestinationSymbol, token) ||
                                EqualsText(x.FeeToken, token))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Network))
            {
                var network = update.Network.Trim();
                swaps = swaps
                    .Where(x => EqualsText(x.SourceNetwork, network) ||
                                EqualsText(x.DestinationNetwork, network) ||
                                x.Transactions.Any(t => EqualsText(t.Network, network)))
                    .ToList();
            }

            return swaps
                .OrderByDescending(x => x.CreatedMoment)
                .ThenBy(x => x.SwapReference)
                .ToList();
        }

        private static ReportTable ToTable(List<Swap> swaps)
        {
            var table = new ReportTable
            {
                Columns =
                [
                    new ReportTableColumn { Header = "Section", Width = 18 },
                    new ReportTableColumn { Header = "Created", Width = 20 },
                    new ReportTableColumn { Header = "Reference", Width = 24 },
                    new ReportTableColumn { Header = "Wallet", Width = 34 },
                    new ReportTableColumn { Header = "Source", Width = 18 },
                    new ReportTableColumn { Header = "SourceAmount", Width = 18 },
                    new ReportTableColumn { Header = "UsdVolume", Width = 18 },
                    new ReportTableColumn { Header = "Destination", Width = 18 },
                    new ReportTableColumn { Header = "DestinationAmount", Width = 18 },
                    new ReportTableColumn { Header = "FeeToken", Width = 14 },
                    new ReportTableColumn { Header = "Fee", Width = 14 },
                    new ReportTableColumn { Header = "FeeUsd", Width = 14 },
                    new ReportTableColumn { Header = "Hash", Width = 34 },
                    new ReportTableColumn { Header = "State", Width = 14 }
                ],
                Rows = []
            };

            foreach (var point in BuildVolume(swaps, PeriodBucketer.Day))
                table.Rows.Add(Row("DailyVolume", point.PeriodStart.ToString("yyyy-MM-dd"), string.Empty, string.Empty, string.Empty, string.Empty, point.UsdVolume, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, point.SwapCount));

            foreach (var point in BuildVolume(swaps, PeriodBucketer.Month))
                table.Rows.Add(Row("MonthlyVolume", point.PeriodStart.ToString("yyyy-MM"), string.Empty, string.Empty, string.Empty, string.Empty, point.UsdVolume, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, point.SwapCount));

            foreach (var fee in BuildFeeRevenue(swaps))
                table.Rows.Add(Row("FeeRevenue", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, fee.FeeToken, fee.NativeAmount, fee.UsdValue, string.Empty, fee.SwapCount));

            foreach (var user in BuildTopUsers(swaps))
                table.Rows.Add(Row("TopUser", string.Empty, string.Empty, user.WalletAddress, string.Empty, string.Empty, user.UsdVolume, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, user.UserPublicKey, user.SwapCount));

            foreach (var token in BuildMostTradedTokens(swaps))
                table.Rows.Add(Row("MostTradedToken", string.Empty, token.Token, string.Empty, token.Token, token.NativeVolume, token.UsdVolume, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, token.SwapCount));

            foreach (var swap in swaps)
            {
                var row = ToRow(swap);
                table.Rows.Add(Row(
                    "Swap",
                    row.CreatedMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                    row.SwapReference,
                    row.WalletAddress,
                    $"{row.SourceSymbol}/{row.SourceNetwork}",
                    row.SourceAmount,
                    row.UsdVolume,
                    $"{row.DestinationSymbol}/{row.DestinationNetwork}",
                    row.DestinationAmount,
                    row.FeeToken,
                    row.Fee,
                    row.FeeUsdValue,
                    row.RegisterHash,
                    row.State));
            }

            return table;
        }

        private static List<SwapVolumePointResult> BuildVolume(List<Swap> swaps, Func<DateTime, DateTime> bucket)
            => swaps
                .GroupBy(x => bucket(x.CreatedMoment))
                .OrderBy(x => x.Key)
                .Select(x => new SwapVolumePointResult
                {
                    PeriodStart = x.Key,
                    UsdVolume = x.Sum(s => UsdValuationHelper.TokenValue(s.SourceAmount, s.SourceTokenPrice)),
                    SwapCount = x.Count()
                })
                .ToList();

        private static List<SwapFeeRevenueResult> BuildFeeRevenue(List<Swap> swaps)
            => swaps
                .Where(x => !string.IsNullOrWhiteSpace(x.FeeToken))
                .GroupBy(x => x.FeeToken.Trim().ToUpperInvariant())
                .OrderBy(x => x.Key)
                .Select(x => new SwapFeeRevenueResult
                {
                    FeeToken = x.Key,
                    NativeAmount = x.Sum(s => s.Fee),
                    UsdValue = SumNullable(x.Select(s => FeeUsdValue(s))),
                    SwapCount = x.Count(),
                    UsdDerivableCount = x.Count(s => FeeUsdValue(s).HasValue)
                })
                .ToList();

        private static List<SwapTopUserResult> BuildTopUsers(List<Swap> swaps)
            => swaps
                .GroupBy(x => string.IsNullOrWhiteSpace(x.UserPublicKey) ? x.WalletAddress : x.UserPublicKey)
                .OrderByDescending(x => x.Sum(s => UsdValuationHelper.TokenValue(s.SourceAmount, s.SourceTokenPrice)))
                .ThenBy(x => x.Key)
                .Take(20)
                .Select(x => new SwapTopUserResult
                {
                    WalletAddress = x.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.WalletAddress))?.WalletAddress,
                    UserPublicKey = x.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.UserPublicKey))?.UserPublicKey,
                    UsdVolume = x.Sum(s => UsdValuationHelper.TokenValue(s.SourceAmount, s.SourceTokenPrice)),
                    SwapCount = x.Count()
                })
                .ToList();

        private static List<SwapTokenVolumeResult> BuildMostTradedTokens(List<Swap> swaps)
            => swaps
                .SelectMany(x => new[]
                {
                    new { Token = x.SourceSymbol, NativeVolume = x.SourceAmount, UsdVolume = UsdValuationHelper.TokenValue(x.SourceAmount, x.SourceTokenPrice) },
                    new { Token = x.DestinationSymbol, NativeVolume = x.DestinationAmount, UsdVolume = UsdValuationHelper.TokenValue(x.DestinationAmount, x.DestinationTokenPrice) }
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Token))
                .GroupBy(x => x.Token.Trim().ToUpperInvariant())
                .OrderByDescending(x => x.Sum(t => t.UsdVolume))
                .ThenBy(x => x.Key)
                .Take(20)
                .Select(x => new SwapTokenVolumeResult
                {
                    Token = x.Key,
                    NativeVolume = x.Sum(t => t.NativeVolume),
                    UsdVolume = x.Sum(t => t.UsdVolume),
                    SwapCount = x.Count()
                })
                .ToList();

        private static SwapReportRowResult ToRow(Swap swap)
            => new()
            {
                CreatedMoment = swap.CreatedMoment,
                SwapReference = swap.SwapReference,
                UserPublicKey = swap.UserPublicKey,
                WalletAddress = swap.WalletAddress,
                SourceNetwork = swap.SourceNetwork,
                SourceSymbol = swap.SourceSymbol,
                SourceAmount = swap.SourceAmount,
                SourceTokenPrice = swap.SourceTokenPrice,
                UsdVolume = UsdValuationHelper.TokenValue(swap.SourceAmount, swap.SourceTokenPrice),
                DestinationNetwork = swap.DestinationNetwork,
                DestinationSymbol = swap.DestinationSymbol,
                DestinationAmount = swap.DestinationAmount,
                DestinationTokenPrice = swap.DestinationTokenPrice,
                FeeToken = swap.FeeToken,
                Fee = swap.Fee,
                FeeUsdValue = FeeUsdValue(swap),
                RegisterHash = swap.RegisterHash,
                RegisterMoment = swap.RegisterMoment,
                RefundHash = swap.SwapRefundRegisterHash,
                RefundMoment = swap.SwapRefundRegisterMoment,
                State = swap.State.ToString()
            };

        private static decimal? FeeUsdValue(Swap swap)
        {
            var price = FeeTokenPrice(swap);
            return price.HasValue ? swap.Fee * price.Value : null;
        }

        private static decimal? FeeTokenPrice(Swap swap)
        {
            if (string.IsNullOrWhiteSpace(swap.FeeToken))
                return null;

            if (EqualsText(swap.FeeToken, swap.SourceSymbol))
                return swap.SourceTokenPrice;

            if (EqualsText(swap.FeeToken, swap.DestinationSymbol))
                return swap.DestinationTokenPrice;

            return null;
        }

        private static decimal? SumNullable(IEnumerable<decimal?> values)
        {
            decimal sum = 0;
            var hasValue = false;

            foreach (var value in values)
            {
                if (!value.HasValue)
                    continue;

                sum += value.Value;
                hasValue = true;
            }

            return hasValue ? sum : null;
        }

        private static bool Contains(string value, string term)
            => !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static bool EqualsText(string value, string term)
            => string.Equals(value?.Trim(), term?.Trim(), StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<object> Row(
            object section,
            object created,
            object reference,
            object wallet,
            object source,
            object sourceAmount,
            object usdVolume,
            object destination,
            object destinationAmount,
            object feeToken,
            object fee,
            object feeUsd,
            object hash,
            object state)
            => [section, created, reference, wallet, source, sourceAmount, usdVolume, destination, destinationAmount, feeToken, fee, feeUsd, hash, state];
    }
}
