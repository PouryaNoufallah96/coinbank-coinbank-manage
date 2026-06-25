# Risk Map

<!-- rz-review:generated:start -->
## Highest-Risk Areas

- Changes to Backend service entry points, because they affect core behavior.
- Setup and refresh code that writes files into Target Projects, because mistakes can overwrite human-owned Project Knowledge.
- External boundaries (routes, CLI, Git provider, AI provider, webhooks), because they handle untrusted input and side effects.
- Shared state and persistence, because they control deduplication, idempotency, and cross-run correctness.
- Output construction (review prompts, schemas, posted comments), because errors propagate to every Review Finding.

## Review Bias

- Be strict about anything that can increase redundant or low-confidence comments.
- Be strict about anything that can overwrite human-owned Project Knowledge or human-edited Review Config.
- Be strict about behavior drift between CLI, API command, and webhook paths.
- Be conservative about adding new runtime dependencies or hidden background behavior.
<!-- rz-review:generated:end -->

<!-- rz-review:human:start -->
## Human Risk Notes

List the specific files, modules, or workflows reviewers should watch most closely in this Target Project, and why each one is high risk.
<!-- rz-review:human:end -->
