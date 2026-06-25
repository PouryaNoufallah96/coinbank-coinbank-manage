# Codebase Map

Dependency chain:

```text
CoinBank.Manage -> CoinBank.Services -> CoinBank.Domain -> CoinBank.Utilities
```

| Layer | Path | Contents |
| --- | --- | --- |
| Host | `CoinBank.Manage/` | `Program.cs`, `Controllers/V1/`, host middleware/config, appsettings |
| Services | `CoinBank.Services/` | `_User`, `_Report`, `_Log` |
| Domain | `CoinBank.Domain/` | Shared CoinBank Mongo collections and Monjo repositories |
| Utilities | `CoinBank.Utilities/` | API envelope, exceptions, auth, JWT, permissions, Monjo, DI, settings |

## Admin Auth

- Controller: `CoinBank.Manage/Controllers/V1/AuthController.cs`
- Service: `CoinBank.Services/_User/UserService.cs`
- DTO: `CoinBank.Services/_User/DTOs/Updates/AdminLoginUpdate.cs`
- Admin user: existing `Users` collection, `Role.Admin`, seeded from `JwtServiceSettings`.

## Reports

- Controllers: `DashboardController`, `SwapReportController`, `TreasuryController`,
  `TokenReleaseController`, `PreSaleReportController`, `UserReportController`,
  `FinancialController`.
- Services and DTOs: `CoinBank.Services/_Report/`.
- Exports: `CoinBank.Services/_Report/Exports/`.

## Add/change

| Task | Edit |
| --- | --- |
| New report endpoint | Add controller action in `CoinBank.Manage/Controllers/V1/`, DTO/service logic in `_Report` |
| New admin auth behavior | Change `_User/UserService.cs` and `AuthController.cs` |
| Repository/index change | Change `CoinBank.Domain/Repositories/*Repository.cs` |
| Permission change | Change `CoinBank.Utilities/Permissions/Permissions.cs` |
| Host pipeline change | Change `CoinBank.Manage/Program.cs` or `CoinBank.Manage/Utilities/Middlewares/` |

There is no on-chain runtime, SignalR runtime, or TRON sidecar in this manage sibling.
