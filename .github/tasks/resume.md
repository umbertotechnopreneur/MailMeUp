# Restart checkpoint — 2026-09-05

🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩⬜⬜⬜⬜⬜⬜⬜ **12.5% verified · 87.5% awaiting verification or later work**

**Current phase:** provider registrations and real-account verification for phases 2–5. Edge extension communication is being repaired separately.

## Saved state

- The MCP source exposes discovery plus `search_mail`, `read_mail`, `list_calendars`, `search_events` and `read_event`. Local CLI source adds provider setup, account connection, scope selection and local removal.
- Public provider IDs are stored in `provider-settings.json`. Google client secrets and per-account tokens use protected slots; Microsoft uses a protected MSAL multi-account cache. SQLite stores non-secret account metadata and read categories.
- Provider reads use short in-memory references, bounded text and coverage reporting. Release build, 16 tests, Windows protected storage and the seven-tool MCP smoke test pass locally.
- The [MVP plan](../../docs/MVP_PLAN.md) and [app registration guide](../../docs/APP_REGISTRATION.md) are saved. Foundation validation recorded earlier does not validate future work.
- The Google Cloud project `mailmeup` exists (project number `669810524015`). Its Desktop client file was imported locally on 2026-09-05 and the protected-secret status is healthy. Gmail/Calendar API, external test audience and publishing state were not independently verified. Microsoft registration remains pending. No account token was obtained.
- The provider setup/sign-in/read source, tests and dependency locks are committed locally. Real provider and clean Windows x64 checks remain.

## Resume here

1. Confirm the Google project has Gmail and Calendar APIs enabled, remains External in Testing and lists the intended test users. Do not publish it.
2. Connect one authorized Google test account and confirm the consent screen requests only the six documented identity, Gmail-read and Calendar-read scopes.
3. Create the Microsoft public desktop registration for personal and work accounts and record its Application (client) ID.
4. Connect one authorized test account at a time. Verify restart, identity separation, scope choices and local removal.
5. Compare provider results with the known pilot messages and appointments. Keep all provider operations read-only. Google installed apps require a fresh consent flow when changing permissions.

## Working agreement

- Conversation: Italian. Repository content and runtime text: English. Keep documentation short.
- Follow [AGENTS.md](../../AGENTS.md). No tests, builds, formatters, smoke tests, preflight, CI dispatch or other checks unless the owner explicitly requests them. Say which checks would be useful at handoff.
- Keep tokens and downloaded client configuration out of Git, SQLite metadata, logs and chat. Do not claim completed sign-in or real-account testing before it happens.
- Browser setup and provider registration work can continue independently of implementation.
