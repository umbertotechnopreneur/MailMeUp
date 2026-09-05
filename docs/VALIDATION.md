# Foundation validation

Validation uses synthetic data and no provider credentials. Last updated: 2026-09-05.

## Verified locally on Windows x64

- .NET 10 Release build: zero warnings and zero errors.
- Nine tests: account persistence, same-address provider isolation, parameterized input, updates, concurrent initialization, schema rejection, relative paths and truthful readiness.
- Process-level smoke test: help/version/status/accounts, invalid-command exit behavior, real stdio initialization, tool discovery, read-only annotations, tool calls and unavailable-tool errors.
- Discovery against a fresh data directory creates no files.
- Two generated PNGs inspected for concept clarity and readable English; originals copied into the repository.

## Remaining foundation verification

- Full repository validation, native portable package smoke and remote CI/release rehearsal are being completed.

## Outside this milestone

No Google/Microsoft authorization, token vault, real-mail operation or native credential-store integration has been tested because those features are not implemented. No release has been published. Cross-architecture compilation must not be represented as native execution.
