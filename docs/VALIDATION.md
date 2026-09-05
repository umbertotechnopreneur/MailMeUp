# Validation

Checked locally on Windows on 2026-09-05. Automated checks used synthetic `example.test` data. A real Google Desktop client file was later imported with command output suppressed; no provider account was connected.

## Passed

- Locked dependency restore, formatting check and Release build with zero warnings or errors.
- Sixteen account, storage, provider setup, protected credential, cross-account mail, calendar, pagination and readiness tests.
- Windows protected storage round-trip, deletion and plaintext-file exclusion using a synthetic token.
- Real CLI/MCP process checks for all seven tools, empty first-run reads, invalid references and read-only annotations.
- Dependency inventory, local-data rules and documentation links.
- Google Desktop client validation and import into Windows-protected storage. The source file stayed outside the repository and no credential value was printed.

Earlier foundation checks also produced six portable packages and ran CI on Windows, Linux and macOS. The first package rehearsal is recorded in [this successful run](https://github.com/umbertotechnopreneur/MailMeUp/actions/runs/33943689252).

## Limits

The current read-only provider source passes Windows x64 package smoke tests before and after ZIP extraction. Windows ARM64 publishes successfully but cannot run on the x64 host. macOS and Linux are not tested because no machines are available; the current MVP makes no support claim for them.

Real Google/Microsoft sign-in, account-token persistence and provider result comparison still require authorized pilot accounts. Google cloud-side consent settings were not independently verified. Provider scope remains read-only. No release was published.

## Current source changes

Provider setup, account removal, cross-account aggregation and read-only mail/calendar MCP contracts now pass local automated checks. Browser sign-in and real provider reads remain unverified.
