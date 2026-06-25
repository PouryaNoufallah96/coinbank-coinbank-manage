using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using MongoDB.Driver;
using Utilities.MongoDatabase;
using Utilities.MongoDatabase.Contracts;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Domain.Repositories
{
    public class StakeRepository(IMonjoConnection connection) : MonjoRepository<Stake>(connection), IStakeRepository, ISingletonDependency
    {
        protected override void Configure()
        {
            CreateIndexMany([
                new CreateIndexModel<Stake>(
                    Builders<Stake>.IndexKeys
                        .Ascending(x => x.State)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_stake_state_created" }),
                new CreateIndexModel<Stake>(
                    Builders<Stake>.IndexKeys
                        .Ascending(x => x.WalletAddress)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_stake_wallet_created" }),
                new CreateIndexModel<Stake>(
                    Builders<Stake>.IndexKeys
                        .Ascending(x => x.TokenSymbol)
                        .Ascending(x => x.TokenNetworkName)
                        .Descending(x => x.StartMoment),
                    new CreateIndexOptions { Name = "ix_report_stake_token_network_start" }),
                new CreateIndexModel<Stake>(
                    Builders<Stake>.IndexKeys
                        .Ascending(x => x.EndMoment)
                        .Ascending(x => x.State),
                    new CreateIndexOptions { Name = "ix_report_stake_end_state" })
            ]);
        }
    }
}
