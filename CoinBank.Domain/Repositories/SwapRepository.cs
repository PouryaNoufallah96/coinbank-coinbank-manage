using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using MongoDB.Driver;
using Utilities.MongoDatabase;
using Utilities.MongoDatabase.Contracts;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Domain.Repositories
{
    public class SwapRepository(IMonjoConnection connection) : MonjoRepository<Swap>(connection), ISwapRepository, ISingletonDependency
    {
        protected override void Configure()
        {
            CreateIndexMany([
                new CreateIndexModel<Swap>(
                    Builders<Swap>.IndexKeys
                        .Ascending(x => x.State)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_swap_state_created" }),
                new CreateIndexModel<Swap>(
                    Builders<Swap>.IndexKeys
                        .Ascending(x => x.WalletAddress)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_swap_wallet_created" }),
                new CreateIndexModel<Swap>(
                    Builders<Swap>.IndexKeys
                        .Ascending(x => x.SourceSymbol)
                        .Ascending(x => x.DestinationSymbol)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_swap_symbols_created" }),
                new CreateIndexModel<Swap>(
                    Builders<Swap>.IndexKeys
                        .Ascending(x => x.SourceNetwork)
                        .Ascending(x => x.DestinationNetwork)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_swap_networks_created" })
            ]);
        }
    }
}
