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
    public interface IPreSaleReportService
    {
        Task<PreSaleReportResult> GetPreSalesAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportPreSalesExcelAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportPreSalesCsvAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default);
        Task<ReportExportResult> ExportPreSalesPdfAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default);
    }

    public class PreSaleReportService(
        IPreSaleRepository _preSaleRepository,
        IPreSaleOrderRepository _preSaleOrderRepository,
        IReportExcelRenderer _excelRenderer,
        IReportCsvRenderer _csvRenderer,
        IReportPdfRenderer _pdfRenderer) : IPreSaleReportService, IScopedDependency
    {
        public async Task<PreSaleReportResult> GetPreSalesAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            update ??= new PreSaleReportQueryUpdate();
            ReportLimits.Enforce(update);

            var allPurchasedOrders = await GetPurchasedOrdersAsync(cancellationToken);
            var filteredOrders = FilterOrders(allPurchasedOrders, update);
            var presales = await GetFilteredPreSalesAsync(update, cancellationToken);
            var buyers = filteredOrders.Select(ToBuyerRow).ToList();
            var page = update.Pagination;

            return new PreSaleReportResult
            {
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Summary = BuildSummary(filteredOrders),
                Projects = BuildProjects(presales, allPurchasedOrders, update),
                Buyers = new ReportPageResult<PreSaleBuyerReportRowResult>
                {
                    TotalCount = buyers.Count,
                    PageCount = (int)Math.Ceiling(buyers.Count / (double)page.Size),
                    Data = buyers.Skip((page.Page - 1) * page.Size).Take(page.Size).ToList()
                },
                DailySales = BuildSalesVolume(filteredOrders, PeriodBucketer.Day),
                MonthlySales = BuildSalesVolume(filteredOrders, PeriodBucketer.Month),
                TokenRanking = BuildTokenRanking(filteredOrders)
            };
        }

        public async Task<ReportExportResult> ExportPreSalesExcelAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _excelRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportPreSalesCsvAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.csv",
                ContentType = "text/csv",
                Content = _csvRenderer.Render(metadata, table)
            };
        }

        public async Task<ReportExportResult> ExportPreSalesPdfAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken = default)
        {
            var (metadata, table) = await BuildExportAsync(update, cancellationToken);

            return new ReportExportResult
            {
                FileName = $"{metadata.FileName}.pdf",
                ContentType = "application/pdf",
                Content = _pdfRenderer.Render(metadata, table)
            };
        }

        private async Task<(ExportMetadata Metadata, ReportTable Table)> BuildExportAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken)
        {
            update ??= new PreSaleReportQueryUpdate();
            ReportLimits.Enforce(update);

            var allPurchasedOrders = await GetPurchasedOrdersAsync(cancellationToken);
            var filteredOrders = FilterOrders(allPurchasedOrders, update);
            var presales = await GetFilteredPreSalesAsync(update, cancellationToken);

            var metadata = new ExportMetadata
            {
                Title = "Pre-Sale Report",
                FileName = $"pre-sale-report-{DateTime.UtcNow:yyyyMMddHHmmss}",
                FromMoment = update.FromMoment,
                ToMoment = update.ToMoment,
                Filters =
                [
                    new ExportFilterText { Label = "From", Value = update.FromMoment == default ? "All" : update.FromMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "To", Value = update.ToMoment == default ? "All" : update.ToMoment.ToString("yyyy-MM-dd") },
                    new ExportFilterText { Label = "Wallet", Value = string.IsNullOrWhiteSpace(update.Wallet) ? "All" : update.Wallet },
                    new ExportFilterText { Label = "Token", Value = string.IsNullOrWhiteSpace(update.Token) ? "All" : update.Token }
                ]
            };

            var table = ToTable(presales, allPurchasedOrders, filteredOrders, update);
            ReportLimits.EnforceExportRowCap(table.Rows.Count);

            return (metadata, table);
        }

        private async Task<List<PreSaleOrder>> GetPurchasedOrdersAsync(CancellationToken cancellationToken)
        {
            return await _preSaleOrderRepository.AsQueryable()
                .Where(x => x.State == PreSaleOrderState.InProgress ||
                            x.State == PreSaleOrderState.Completed)
                .ToListAsync(cancellationToken);
        }

        private async Task<List<PreSale>> GetFilteredPreSalesAsync(PreSaleReportQueryUpdate update, CancellationToken cancellationToken)
        {
            var presales = await _preSaleRepository.AsQueryable().ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                presales = presales
                    .Where(x => EqualsText(x.Symbol, token) ||
                                Contains(x.Name, token) ||
                                EqualsText(x.PreSaleReference, token))
                    .ToList();
            }

            return presales
                .OrderBy(x => x.Symbol)
                .ThenBy(x => x.PreSaleReference)
                .ToList();
        }

        private static List<PreSaleOrder> FilterOrders(List<PreSaleOrder> orders, PreSaleReportQueryUpdate update)
        {
            var filtered = orders;

            if (update.FromMoment != default)
                filtered = filtered.Where(x => x.CreatedMoment >= update.FromMoment).ToList();

            if (update.ToMoment != default)
                filtered = filtered.Where(x => x.CreatedMoment <= update.ToMoment).ToList();

            if (!string.IsNullOrWhiteSpace(update.Wallet))
            {
                var wallet = update.Wallet.Trim();
                filtered = filtered
                    .Where(x => Contains(x.WalletAddress, wallet) ||
                                Contains(x.UserPublicKey, wallet))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                filtered = filtered
                    .Where(x => EqualsText(x.Symbol, token) ||
                                Contains(x.Name, token) ||
                                EqualsText(x.PreSaleReference, token))
                    .ToList();
            }

            return filtered
                .OrderByDescending(x => x.CreatedMoment)
                .ThenBy(x => x.PreSaleOrderReference)
                .ToList();
        }

        private static PreSaleReportSummaryResult BuildSummary(List<PreSaleOrder> orders)
            => new()
            {
                OrderCount = orders.Count,
                BuyerCount = BuyerCount(orders),
                SoldTokenAmount = orders.Sum(x => x.ReceivingTokenAmount),
                SalesUsdValue = orders.Sum(SalesUsdValue)
            };

        private static List<PreSaleProjectReportResult> BuildProjects(
            List<PreSale> presales,
            List<PreSaleOrder> allPurchasedOrders,
            PreSaleReportQueryUpdate update)
        {
            var projectRows = presales
                .Select(preSale => ToProject(preSale, allPurchasedOrders
                    .Where(x => EqualsText(x.PreSaleReference, preSale.PreSaleReference))
                    .ToList()))
                .ToList();

            var knownReferences = presales
                .Where(x => !string.IsNullOrWhiteSpace(x.PreSaleReference))
                .Select(x => x.PreSaleReference.Trim().ToLowerInvariant())
                .ToHashSet();

            var orderOnlyProjects = allPurchasedOrders
                .Where(x => string.IsNullOrWhiteSpace(x.PreSaleReference) ||
                            !knownReferences.Contains(x.PreSaleReference.Trim().ToLowerInvariant()))
                .GroupBy(x => x.PreSaleReference)
                .Select(x => ToProject(x.Key, x.ToList()))
                .ToList();

            projectRows.AddRange(orderOnlyProjects);

            if (!string.IsNullOrWhiteSpace(update.Token))
            {
                var token = update.Token.Trim();
                projectRows = projectRows
                    .Where(x => EqualsText(x.TokenSymbol, token) ||
                                Contains(x.TokenName, token) ||
                                EqualsText(x.PreSaleReference, token))
                    .ToList();
            }

            return projectRows
                .OrderByDescending(x => x.SalesUsdValue)
                .ThenBy(x => x.TokenSymbol)
                .ThenBy(x => x.PreSaleReference)
                .ToList();
        }

        private static PreSaleProjectReportResult ToProject(PreSale preSale, List<PreSaleOrder> orders)
        {
            var sold = orders.Sum(x => x.ReceivingTokenAmount);

            return new PreSaleProjectReportResult
            {
                PreSaleReference = preSale.PreSaleReference,
                TokenSymbol = preSale.Symbol,
                TokenName = preSale.Name,
                TokenPreSalePrice = preSale.Price,
                TotalSupply = preSale.TotalSupply,
                SoldTokenAmount = sold,
                RemainingTokenAmount = Math.Max(0, preSale.TotalSupply - sold),
                SalesUsdValue = orders.Sum(SalesUsdValue),
                BuyerCount = BuyerCount(orders),
                OrderCount = orders.Count,
                StartSellingAt = preSale.StartSellingAt,
                EndSellingAt = preSale.EndSellingAt,
                State = preSale.State.ToString()
            };
        }

        private static PreSaleProjectReportResult ToProject(string preSaleReference, List<PreSaleOrder> orders)
        {
            var first = orders
                .OrderBy(x => x.CreatedMoment)
                .FirstOrDefault();

            return new PreSaleProjectReportResult
            {
                PreSaleReference = preSaleReference,
                TokenSymbol = first?.Symbol,
                TokenName = first?.Name,
                TokenPreSalePrice = first?.TokenPreSalePrice ?? 0,
                TotalSupply = 0,
                SoldTokenAmount = orders.Sum(x => x.ReceivingTokenAmount),
                RemainingTokenAmount = 0,
                SalesUsdValue = orders.Sum(SalesUsdValue),
                BuyerCount = BuyerCount(orders),
                OrderCount = orders.Count,
                State = "Unknown"
            };
        }

        private static List<PreSaleSalesVolumeResult> BuildSalesVolume(List<PreSaleOrder> orders, Func<DateTime, DateTime> bucket)
            => orders
                .GroupBy(x => bucket(x.CreatedMoment))
                .OrderBy(x => x.Key)
                .Select(x => new PreSaleSalesVolumeResult
                {
                    PeriodStart = x.Key,
                    SoldTokenAmount = x.Sum(o => o.ReceivingTokenAmount),
                    SalesUsdValue = x.Sum(SalesUsdValue),
                    OrderCount = x.Count(),
                    BuyerCount = BuyerCount(x.ToList())
                })
                .ToList();

        private static List<PreSaleTokenRankingResult> BuildTokenRanking(List<PreSaleOrder> orders)
            => orders
                .GroupBy(x => new
                {
                    Reference = x.PreSaleReference,
                    Symbol = string.IsNullOrWhiteSpace(x.Symbol) ? string.Empty : x.Symbol.Trim().ToUpperInvariant(),
                    Name = string.IsNullOrWhiteSpace(x.Name) ? string.Empty : x.Name.Trim()
                })
                .Select(x => new PreSaleTokenRankingResult
                {
                    PreSaleReference = x.Key.Reference,
                    TokenSymbol = x.Key.Symbol,
                    TokenName = x.Key.Name,
                    SoldTokenAmount = x.Sum(o => o.ReceivingTokenAmount),
                    SalesUsdValue = x.Sum(SalesUsdValue),
                    BuyerCount = BuyerCount(x.ToList()),
                    OrderCount = x.Count()
                })
                .OrderByDescending(x => x.SalesUsdValue)
                .ThenByDescending(x => x.SoldTokenAmount)
                .ThenBy(x => x.TokenSymbol)
                .ToList();

        private static ReportTable ToTable(
            List<PreSale> presales,
            List<PreSaleOrder> allPurchasedOrders,
            List<PreSaleOrder> filteredOrders,
            PreSaleReportQueryUpdate update)
        {
            var summary = BuildSummary(filteredOrders);
            var table = new ReportTable
            {
                Columns =
                [
                    new ReportTableColumn { Header = "Section", Width = 18 },
                    new ReportTableColumn { Header = "Date", Width = 20 },
                    new ReportTableColumn { Header = "Reference", Width = 24 },
                    new ReportTableColumn { Header = "Wallet", Width = 34 },
                    new ReportTableColumn { Header = "Token", Width = 16 },
                    new ReportTableColumn { Header = "Amount", Width = 18 },
                    new ReportTableColumn { Header = "PriceUsdt", Width = 14 },
                    new ReportTableColumn { Header = "SalesUsd", Width = 18 },
                    new ReportTableColumn { Header = "BuyerCount", Width = 14 },
                    new ReportTableColumn { Header = "OrderCount", Width = 14 },
                    new ReportTableColumn { Header = "TotalSupply", Width = 18 },
                    new ReportTableColumn { Header = "Remaining", Width = 18 },
                    new ReportTableColumn { Header = "PaymentToken", Width = 16 },
                    new ReportTableColumn { Header = "PaymentAmount", Width = 18 },
                    new ReportTableColumn { Header = "State", Width = 14 }
                ],
                Rows = []
            };

            table.Rows.Add(Row("Summary", string.Empty, "PurchasedOrders", string.Empty, string.Empty, summary.SoldTokenAmount, string.Empty, summary.SalesUsdValue, summary.BuyerCount, summary.OrderCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var project in BuildProjects(presales, allPurchasedOrders, update))
                table.Rows.Add(Row("Project", string.Empty, project.PreSaleReference, string.Empty, project.TokenSymbol, project.SoldTokenAmount, project.TokenPreSalePrice, project.SalesUsdValue, project.BuyerCount, project.OrderCount, project.TotalSupply, project.RemainingTokenAmount, string.Empty, string.Empty, project.State));

            foreach (var point in BuildSalesVolume(filteredOrders, PeriodBucketer.Day))
                table.Rows.Add(Row("DailySales", point.PeriodStart.ToString("yyyy-MM-dd"), string.Empty, string.Empty, string.Empty, point.SoldTokenAmount, string.Empty, point.SalesUsdValue, point.BuyerCount, point.OrderCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var point in BuildSalesVolume(filteredOrders, PeriodBucketer.Month))
                table.Rows.Add(Row("MonthlySales", point.PeriodStart.ToString("yyyy-MM"), string.Empty, string.Empty, string.Empty, point.SoldTokenAmount, string.Empty, point.SalesUsdValue, point.BuyerCount, point.OrderCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var token in BuildTokenRanking(filteredOrders))
                table.Rows.Add(Row("TokenRanking", string.Empty, token.PreSaleReference, string.Empty, token.TokenSymbol, token.SoldTokenAmount, string.Empty, token.SalesUsdValue, token.BuyerCount, token.OrderCount, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

            foreach (var order in filteredOrders)
            {
                table.Rows.Add(Row(
                    "Buyer",
                    order.CreatedMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                    order.PreSaleOrderReference,
                    order.WalletAddress,
                    order.Symbol,
                    order.ReceivingTokenAmount,
                    order.TokenPreSalePrice,
                    SalesUsdValue(order),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    order.PaymentToken,
                    order.PaymentTokenAmount,
                    order.State));
            }

            return table;
        }

        private static PreSaleBuyerReportRowResult ToBuyerRow(PreSaleOrder order)
            => new()
            {
                CreatedMoment = order.CreatedMoment,
                PreSaleOrderReference = order.PreSaleOrderReference,
                PreSaleReference = order.PreSaleReference,
                TokenSymbol = order.Symbol,
                TokenName = order.Name,
                WalletAddress = order.WalletAddress,
                UserPublicKey = order.UserPublicKey,
                ReceivingTokenAmount = order.ReceivingTokenAmount,
                TokenPreSalePrice = order.TokenPreSalePrice,
                SalesUsdValue = SalesUsdValue(order),
                PaymentToken = order.PaymentToken,
                PaymentTokenAmount = order.PaymentTokenAmount,
                State = order.State.ToString(),
                RegisterHash = order.RegisterHash,
                RegisterMoment = order.RegisterMoment
            };

        private static decimal SalesUsdValue(PreSaleOrder order)
            => UsdValuationHelper.TokenValue(order.ReceivingTokenAmount, order.TokenPreSalePrice);

        private static int BuyerCount(IEnumerable<PreSaleOrder> orders)
            => orders
                .Where(x => !string.IsNullOrWhiteSpace(x.WalletAddress) ||
                            !string.IsNullOrWhiteSpace(x.UserPublicKey))
                .Select(x => !string.IsNullOrWhiteSpace(x.WalletAddress)
                    ? x.WalletAddress.Trim().ToLowerInvariant()
                    : x.UserPublicKey.Trim().ToLowerInvariant())
                .Distinct()
                .Count();

        private static IReadOnlyList<object> Row(params object[] values)
            => values;

        private static bool Contains(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);

        private static bool EqualsText(string source, string value)
            => !string.IsNullOrWhiteSpace(source) &&
               string.Equals(source.Trim(), value, StringComparison.OrdinalIgnoreCase);
    }
}
