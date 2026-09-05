# Changelog

## Unreleased

- Add a concise MVP plan with progress indicators and a provider app registration guide.
- Record the owner's requirement to run tests and other checks only when explicitly requested.
- Add local Google and Microsoft provider app setup commands.
- Store public provider IDs separately from credentials and protect the Google Desktop client secret with the operating system.
- Add interactive multi-account sign-in, mail/calendar scope choices, protected Google token slots and a protected MSAL cache.
- Add local account removal and SQLite schema version 2 for non-secret granted read categories.
- Add compact cross-account Gmail/Microsoft mail search and bounded selected-message reading.
- Add Google/Microsoft calendar discovery, combined agenda search and bounded appointment details.
- Keep provider identifiers behind short in-memory references and report partial account coverage.
- Extend dependency notice generation to support legacy NuGet license metadata.

Provider setup, account connection, mail and calendar source now pass local build, tests and MCP smoke checks. Real provider and clean Windows installation checks remain. macOS and Linux are outside current validation.

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
