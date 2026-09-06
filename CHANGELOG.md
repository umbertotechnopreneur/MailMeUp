# Changelog

## Unreleased

- Fix the packaged WinUI startup crash when an account database already exists by using SQLitePCL directly instead of triggering the Microsoft.Data.Sqlite application-data probe. Add a synthetic installed-desktop startup smoke test for this regression.
- Auto-format staged C# files before commits, apply style fixes during local validation and keep CI formatting checks read-only.
- Add an English About & Support dialog with a generated MailMeUp banner, app-version copying, creator website and GitHub links, support issues, and an invitation to star the project.

- Add a centered WinUI 3 setup window with a privacy welcome, Google/Microsoft browser sign-in, multiple accounts, sharing choices and guided local Codex plugin installation.
- Keep the setup interface in English, with persistent links to the website and the MailMeUp, Google and Microsoft privacy policies and terms.
- Share application service composition between CLI/MCP and the Windows UI; keep the MCP process separate from the setup window.
- Store local account, mail and calendar sharing separately from OAuth grants, with new UI-connected accounts initially unshared and read restrictions enforced in running MCP sessions.
- Add Windows MSIX packaging with a stable `mailmeup.exe` console app execution alias and optional signing with an existing certificate.
- Ask callers to notify the user in plain English when MailMeUp cannot read a mailbox, including actionable, sanitized error details and partial-coverage reporting.

The desktop, plugin and sharing behavior remains a preview. Windows x64 MSIX `0.1.1.4` was built, signed and installed as an upgrade; synthetic existing-registry startup, published/installed-alias smoke checks and 111 tests passed. The About dialog, generated banner and version-copy action were inspected on the preceding build. Clean-machine installation, UI sign-in and Codex plugin loading remain pending.

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
