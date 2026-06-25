using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using MongoDB.Driver;
using Utilities.MongoDatabase;
using Utilities.MongoDatabase.Contracts;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Domain.Repositories
{
    public class WithdrawalRepository(IMonjoConnection connection) 
        : MonjoRepository<Withdrawal>(connection), IWithdrawalRepository, ISingletonDependency
    {
        protected override void Configure()
        {
            CreateIndexMany([
                new CreateIndexModel<Withdrawal>(
                    Builders<Withdrawal>.IndexKeys
                        .Ascending(x => x.State)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_withdrawal_state_created" }),
                new CreateIndexModel<Withdrawal>(
                    Builders<Withdrawal>.IndexKeys
                        .Ascending(x => x.WalletAddress)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_withdrawal_wallet_created" }),
                new CreateIndexModel<Withdrawal>(
                    Builders<Withdrawal>.IndexKeys
                        .Ascending(x => x.Symbol)
                        .Ascending(x => x.Network)
                        .Ascending(x => x.Type)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_withdrawal_symbol_network_type_created" }),
                new CreateIndexModel<Withdrawal>(
                    Builders<Withdrawal>.IndexKeys
                        .Ascending(x => x.RegisterMoment)
                        .Ascending(x => x.State),
                    new CreateIndexOptions { Name = "ix_report_withdrawal_register_state" })
            ]);
        }
    }
}
