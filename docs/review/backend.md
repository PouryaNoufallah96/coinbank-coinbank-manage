# Backend Review Context

<!-- rz-review:generated:start -->
## Detected Backend Signals

- .NET backend
- Docker

## Review Focus

- Check webhook validation, auth boundaries, external API failures, retries, idempotency, and duplicate processing.
- Check that local persistence remains safe for the intended deployment model.
- Check that AI and Git provider failures produce actionable user-facing errors.
- Check that API, CLI, and webhook paths share behavior instead of drifting.
<!-- rz-review:generated:end -->

<!-- rz-review:human:start -->
## Human Backend Notes

Document service invariants, provider limits, deployment constraints, and data-retention expectations.
<!-- rz-review:human:end -->
