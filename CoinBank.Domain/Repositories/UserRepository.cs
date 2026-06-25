using CoinBank.Domain.Collections;
using CoinBank.Domain.Repositories.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;
using Utilities.MongoDatabase;
using Utilities.MongoDatabase.Contracts;
using static Utilities.Constants.RegisterMode;

namespace CoinBank.Domain.Repositories
{
    public class UserRepository(IMonjoConnection connection) : MonjoRepository<User>(connection), IUserRepository, ISingletonDependency
    {
        protected override void Configure()
        {
            var keys = Builders<User>.IndexKeys.Ascending(u => u.UserName);
            var options = new CreateIndexOptions<User>
            {
                Name = "ux_admin_username",
                Unique = true,
                PartialFilterExpression = new BsonDocument
                {
                    { nameof(User.Role), (int)UserRole.Admin },
                    { nameof(User.IsDeleted), false },
                    { nameof(User.UserName), new BsonDocument("$type", "string") }
                }
            };

            CreateIndexOne(new CreateIndexModel<User>(keys, options));

            CreateIndexMany([
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys
                        .Ascending(x => x.Role)
                        .Ascending(x => x.Status)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_user_role_status_created" }),
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys
                        .Ascending(x => x.EVMWalletAddress)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_user_evm_wallet_created" }),
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys
                        .Ascending(x => x.TronWalletAddress)
                        .Descending(x => x.CreatedMoment),
                    new CreateIndexOptions { Name = "ix_report_user_tron_wallet_created" })
            ]);
        }
    }
}
