# Architecture Review Context

<!-- rz-review:generated:start -->
## Detected Stack

- .NET backend
- Docker

## Architecture Shape

- The project exposes Backend service behavior.
- Source code is organized around these top-level areas:
- .config
- CoinBank.Domain
- CoinBank.Manage
- CoinBank.Services
- CoinBank.Utilities
- docs

## Review Focus

- Check whether changes preserve the existing module boundaries.
- Check whether service, route, CLI, and storage changes keep responsibilities separated.
- Check whether new dependencies are justified by real complexity.
- Check whether generated Project Knowledge still matches changed architecture after large refactors.
<!-- rz-review:generated:end -->

<!-- rz-review:human:start -->
## Human Architecture Notes

Document architectural boundaries, non-obvious tradeoffs, and areas where future Review Findings should be stricter or more forgiving.
<!-- rz-review:human:end -->
