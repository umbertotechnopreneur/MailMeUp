# Validation

Checked locally on Windows on 2026-09-05 using synthetic `example.test` data and no provider credentials.

## Passed

- Locked dependency restore, formatting check and Release build with zero warnings or errors.
- Fifteen account, storage, provider setup, cross-account mail, calendar, pagination and readiness tests.
- Real CLI/MCP process checks for all seven tools, empty first-run reads, invalid references and read-only annotations.
- Dependency inventory, local-data rules and documentation links.

Earlier foundation checks also produced six portable packages and ran CI on Windows, Linux and macOS. The first package rehearsal is recorded in [this successful run](https://github.com/umbertotechnopreneur/MailMeUp/actions/runs/33943689252).

## Limits

The current read-only provider source has passed a native Windows x64 package smoke test. Remote CI and the other current portable packages remain pending. Native package smoke tests run only when the runner matches the target CPU.

Real Google/Microsoft sign-in, OS-protected credential persistence and provider result comparison still require the app registrations and authorized pilot accounts. Provider scope remains read-only. No release was published.

## Current source changes

Provider setup, account removal, cross-account aggregation and read-only mail/calendar MCP contracts now pass local automated checks. Browser sign-in and real provider reads remain unverified.
