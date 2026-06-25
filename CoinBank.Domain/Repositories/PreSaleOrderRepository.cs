using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using MongoDB.Driver;
using Utilities.MongoDatabase;
using Utilities.MongoDatabase.Contracts;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Domain.Repositories
{
    public class PreSaleOrderRepository(IMonjoConnection connection) : 
        MonjoRepository<PreSaleOrder>(connection), IPreSaleOrderRepository, ISingletonDependency
    {
        protected override void Configure()
        {
            CreateIndexMany([
                new CreateIndexModel<PreSaleOrder>(
                    Builders<PreSaleOrder>.IndexKeys
                        .Ascending(x => x.State)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_presale_order_state_created" }),
                new CreateIndexModel<PreSaleOrder>(
                    Builders<PreSaleOrder>.IndexKeys
                        .Ascending(x => x.WalletAddress)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_presale_order_wallet_created" }),
                new CreateIndexModel<PreSaleOrder>(
                    Builders<PreSaleOrder>.IndexKeys
                        .Ascending(x => x.PreSaleReference)
                        .Ascending(x => x.Symbol)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_presale_order_ref_symbol_created" }),
                new CreateIndexModel<PreSaleOrder>(
                    Builders<PreSaleOrder>.IndexKeys
                        .Ascending("ReleaseSchedule.ReleaseDate")
                        .Ascending(x => x.State),
                    new CreateIndexOptions { Name = "ix_report_presale_order_release_state" })
            ]);
        }
    }
}
