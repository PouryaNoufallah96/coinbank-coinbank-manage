# CoinBank Manage

Back-office admin/report API for CoinBank. It reads the same Mongo database as `coinbank.api` but
runs as a separate four-layer sibling patterned after `slt.manage`.

## Domain

**Admin / Reporter**:
A `User` row with `Role=Admin` who authenticates by username/password and receives report
permissions in JWT claims. Like every user it also carries a wallet (`EVMWalletAddress`), since the
shared `Users` collection is read by the public API.
_Avoid_: treating admins as wallet-auth customers; adding a separate admin database when the
shared `Users` collection is the reference pattern.

**ReportView permission**:
Single coarse permission code used to gate every admin report and export endpoint.
_Avoid_: per-section permissions unless the product explicitly asks.

**Admin report**:
Read-only operational/accounting view over existing CoinBank business collections, returned as JSON
and exportable as Excel/CSV/PDF.

**Notional value (USD)**:
Common-unit measure derived from price stored on each business record at the time it occurred, not a
live re-price.

**Treasury Program**:
Admin-reporting name for staking obligations: locked principal, committed/paid reward, reward rate,
and maturity. Same `Stake` and `Withdrawal` collections; not a separate product.

**Treasury custody**:
Accounting view of platform holdings/flows. Amounts come from typed business collections; never from
live wallet-balance snapshots.

## Platform

**Monjo**:
Hand-rolled MongoDB abstraction (`MonjoRepository<T>`, `IMonjoConnection`, `MonjoQuery`,
`[MonjoCollectionName]`). Intentional spelling.

**Soft-delete**:
Deletes set `IsDeleted=true`; reads auto-filter `!IsDeleted`. `RealDeleteManyAsync` is the only hard
delete.

**ApiResult**:
Universal response envelope applied by `[ApiResultFilter]`.

**JWE**:
Encrypted JWT used by the shared auth utilities.

**Signature middleware**:
Request gate requiring `ApplicationId`, `Nonce`, and `Signature` headers.

**RegisterMode marker**:
DI opt-in: `IScopedDependency`, `ITransientDependency`, `ISingletonDependency`,
`ISelfSingletonDependency`, `IHostedDependency`.

This manage runtime intentionally has no public wallet-signature auth flow, on-chain RPC clients,
SignalR hubs, or TRON sidecar.
