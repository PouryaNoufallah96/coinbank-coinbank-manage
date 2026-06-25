using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using MongoDB.Driver;
using Utilities.MongoDatabase;
using Utilities.MongoDatabase.Contracts;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Domain.Repositories
{
    public class TransactionLogRepository(IMonjoConnection connection) : MonjoRepository<TransactionLog>(connection), ITransactionLogRepository, ISingletonDependency
    {
        protected override void Configure()
        {
            CreateIndexMany([
                new CreateIndexModel<TransactionLog>(
                    Builders<TransactionLog>.IndexKeys
                        .Ascending(x => x.Reference)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_tx_reference_created" }),
                new CreateIndexModel<TransactionLog>(
                    Builders<TransactionLog>.IndexKeys
                        .Ascending(x => x.Wallet)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_tx_wallet_created" }),
                new CreateIndexModel<TransactionLog>(
                    Builders<TransactionLog>.IndexKeys
                        .Ascending(x => x.EventType)
                        .Ascending(x => x.Status)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_tx_event_status_created" }),
                new CreateIndexModel<TransactionLog>(
                    Builders<TransactionLog>.IndexKeys
                        .Ascending(x => x.Network)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_tx_network_created" })
            ]);
        }
    }
}
