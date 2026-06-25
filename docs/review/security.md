# Security Review Context

<!-- rz-review:generated:start -->
## Review Focus

- Secrets in code, logs, generated docs, fixtures, or examples; flag committed tokens, keys, and credentials.
- Unsafe shell or command execution: untrusted input reaching exec/spawn/eval or shell-interpolated strings.
- Injection at data boundaries: SQL/NoSQL, template, path traversal, and command injection from external input.
- Authentication and authorization: missing or weak auth checks on routes, CLI actions, and provider calls.
- Webhook validation: signature/token verification, replay protection, and idempotent event handling.
- External input boundaries: validate and bound input from routes, CLI args, Git provider, and AI provider responses before use.
- Provider/API errors: ensure failures do not leak sensitive data into logs or user-facing comments.
- Public telemetry/client DSNs are not credentials by themselves; only flag them when paired with private auth tokens, source-map upload tokens, write/admin tokens, or other credentials.

## Posting Threshold

Security findings still meet the normal evidence threshold. Do not post generic security advice without a concrete changed-code path and a described failure mode.
<!-- rz-review:generated:end -->

<!-- rz-review:human:start -->
## Human Security Notes

Document the highest-risk security boundaries for this Target Project, trusted vs. untrusted input sources, and any accepted exceptions reviewers should not re-flag.
<!-- rz-review:human:end -->
