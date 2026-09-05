# Changelog

## Unreleased

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

Provider setup, account connection, mail, calendar, the CLI and Serilog diagnostics pass the local build, tests and MCP smoke checks. Manual real-provider checks pass across four accounts. Rebuilt packaging and a clean Windows installation check remain. macOS and Linux are outside current validation.

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
