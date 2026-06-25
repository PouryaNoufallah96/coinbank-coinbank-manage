# CoinBank Manage

Back-office .NET 8 API for CoinBank admin authentication, dashboard/report queries, and report
exports. This is a separate sibling of `coinbank.api`, patterned after `slt.manage`, and uses the
same CoinBank Mongo database.

Layering:

```text
CoinBank.Manage -> CoinBank.Services -> CoinBank.Domain -> CoinBank.Utilities
```

Projects:

| Project | Role |
| --- | --- |
| `CoinBank.Manage` | ASP.NET Core host, V1 admin/report controllers, middleware pipeline |
| `CoinBank.Services` | `_User` admin login/seed, `_Report` report/export services, `_Log` |
| `CoinBank.Domain` | Shared CoinBank Mongo collections and Monjo repositories |
| `CoinBank.Utilities` | Cross-cutting API result, auth, JWT, Monjo, DI, settings, permissions |

Build:

```bash
dotnet build CoinBank.Manage.slnx
dotnet build CoinBank.Manage/CoinBank.Manage.csproj -c Release
```

Run locally:

```bash
dotnet run --project CoinBank.Manage
```

Deploy shape:

```bash
dotnet publish CoinBank.Manage/CoinBank.Manage.csproj -c Release -o publish
docker build -t coinbank.manage .
docker-compose up -d
```

This project intentionally excludes public wallet-auth endpoints, on-chain clients, TRON sidecars,
SignalR hubs, and blockchain background schedulers.
