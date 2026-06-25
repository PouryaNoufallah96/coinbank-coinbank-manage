# Convention Review Context

<!-- rz-review:generated:start -->
## Detected Commands

- `build`: `dotnet build CoinBank.Manage.slnx`
- `release-build`: `dotnet build CoinBank.Manage/CoinBank.Manage.csproj -c Release`

## Detected Conventions

- Prefer existing local services, route modules, domain modules, and library helpers before introducing new patterns.
- Keep environment handling centralized through the existing configuration module when one exists.
- Keep Git provider integration code isolated from review orchestration logic.
- Keep AI prompt construction stable where provider-side prefix caching can help.
- Prefer deterministic parsing and filtering before asking the model to reason.

## Review Focus

- Flag duplicated command logic between CLI, API, and webhook paths.
- Flag comments or prompts that create broad, noisy review behavior.
- Flag changes that make local and webhook modes diverge without a clear reason.
<!-- rz-review:generated:end -->

<!-- rz-review:human:start -->
## Human Convention Notes

Add naming, formatting, layering, and workflow rules that should be treated as project-specific requirements.
<!-- rz-review:human:end -->
