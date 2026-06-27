# Security

CoinBank Manage is an admin surface. Treat every endpoint as privileged.

Rules:

- Admin report endpoints require the custom `Utilities.Filters.AuthorizeAttribute` with
  `RequireActiveUser = true`, `RequireAdmin = true`, and `Permissions.ReportView`.
- The seeded admin must carry a wallet: set `AdminWalletAddress` alongside `AdminUserName` /
  `AdminPassword`. The shared `Users` collection is also read by the public API, which expects that
  field to be populated rather than blank.
- Admin credentials, JWT keys, application pre-shared keys, and Mongo credentials must come from
  environment/local untracked config in real deployments.
- Do not read or commit `.env` or `.env.*`.
- Do not log passwords, JWTs, signatures, application secrets, Mongo connection strings, or report
  exports containing user data.
- Keep public wallet-signature auth, on-chain RPC clients, TRON sidecars, and background blockchain
  listeners out of this manage runtime.

The manage host reads the same database as the public API, so report changes can expose real user
and transaction data even when they are read-only.
