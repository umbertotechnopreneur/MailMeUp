# Changelog

## Unreleased

## 0.1.1 — Mail search ergonomics

- Add dedicated unread and received-date-range mail tools with sender/recipient contains and attachment filters; exclude Gmail Spam/Trash and Microsoft Junk/Deleted Items by default.
- Return read status and attachment presence in compact mail results, and run independent account searches with bounded concurrency.
- Preserve existing credentials after failed reconnect validation or metadata persistence, and coordinate credential sessions across processes.
- Bound provider reads and continuation work; report timeouts, removed accounts and incomplete calendar discovery as partial coverage.
- Handle calendar null fields and all-day/time-zone boundaries without silently guessing missing event times.
- Harden the manual provider runner with CI refusal, per-account outcomes and bounded calendar batches.
- Add synthetic recovery, credential-session, calendar boundary and pagination regressions, plus isolated tests for the manual provider runner.
- Keep unit-test execution local on request; CI retains build, isolated protocol smoke and repository checks without live accounts.
- Add a Spectre.Console CLI with a compact linked banner, emoji section dividers, readable results, next steps and sign-in activity feedback, without cards or panels.
- Preserve redirected JSON and add explicit `--json`, `--no-color`, `--no-animation`, configurable log levels and Ctrl+C cancellation.
- Route bounded application diagnostics through `ILogger<T>` and Serilog to stderr for both CLI and MCP.
- Add a concise MVP plan with progress indicators and a provider app registration guide.
- Record the owner's requirement to run tests and other checks only when explicitly requested.
- Add local Google and Microsoft provider app setup commands.
- Store public provider IDs separately from credentials and protect the Google Desktop client secret with the operating system.
- Add interactive multi-account sign-in, mail/calendar scope choices, protected Google token slots and a protected MSAL cache.
- Add local account removal and SQLite schema version 2 for non-secret granted read categories.
- Add compact cross-account Gmail/Microsoft mail search and bounded selected-message reading.
- Add Google/Microsoft calendar discovery, combined agenda search and bounded appointment details.
- Handle nullable Microsoft event location and online-meeting fields during read-only detail retrieval.
- Add an opt-in real-provider runner that enumerates connected accounts dynamically and stays outside CI.
- Keep provider identifiers behind short in-memory references and report partial account coverage.
- Extend dependency notice generation to support legacy NuGet license metadata.

The current Windows source passed 68 .NET tests and 24 manual-runner regressions. Both the development and published executables passed 27 real-provider checks across four accounts, with three skipped event checks per run. The package from `de1fae7` passed smoke checks before and after extraction, and an update preserved all four accounts. Clean Windows installation remains. macOS and Linux are outside current runtime validation.

## 0.1.0-alpha.1 — Foundation

- Establish the .NET 10 solution and separate application, storage, security, provider, MCP and CLI modules.
- Add CLI discovery commands and working `get_status` / `list_accounts` MCP tools over stdio.
- Add SQLite metadata persistence, schema checks and account isolation tests.
- Add CI, portable release automation and protocol smoke tests.
- Document product scope, OAuth/token design, compact search contracts and Codex setup.
- Add the MailMeUp brand guide and generated concept artwork.
- Include Google Calendar and Microsoft appointments in the architecture and roadmap, with separate capability readiness.
- Make the read-only scope explicit in status output, the README and concise product documentation.

Account authentication, credential storage implementations, mail and calendar operations were not included in this foundation release.
