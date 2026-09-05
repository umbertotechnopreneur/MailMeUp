# Restart checkpoint — 2026-09-05

🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩⬜⬜⬜⬜⬜⬜⬜ **12.5% verified · 87.5% awaiting verification or later work**

**Current phase:** provider registrations and real-account verification for phases 2–5. Edge extension communication is being repaired separately.

## Saved state

- The MCP source exposes discovery plus `search_mail`, `read_mail`, `list_calendars`, `search_events` and `read_event`. Local CLI source adds provider setup, account connection, scope selection and local removal.
- Public provider IDs are stored in `provider-settings.json`. Google client secrets and per-account tokens use protected slots; Microsoft uses a protected MSAL multi-account cache. SQLite stores non-secret account metadata and read categories.
- Provider reads use short in-memory references, bounded text and coverage reporting. Release build, 16 tests, Windows protected storage and the seven-tool MCP smoke test pass locally.
- The [MVP plan](../../docs/MVP_PLAN.md) and [app registration guide](../../docs/APP_REGISTRATION.md) are saved. Foundation validation recorded earlier does not validate future work.
- The Google Cloud project `mailmeup` exists (project number `669810524015`). Gmail/Calendar API, OAuth consent and Desktop client completion still need confirmation. Microsoft registration remains pending. No account tokens or client credentials were obtained.
- The provider setup/sign-in/read source, tests and dependency locks are uncommitted. Real provider checks and current portable packaging remain.

## Resume here

1. Finish the Google project configuration: enable Gmail and Calendar APIs, configure an external test audience and create a Desktop OAuth client. Download its JSON privately to `.local/google/client_secret.json`.
2. Create the Microsoft public desktop registration for personal and work accounts and record its Application (client) ID.
3. Connect one authorized test account at a time. Verify restart, identity separation, scope choices and local removal.
4. Compare provider results with the known pilot messages and appointments. Keep all provider operations read-only. Google installed apps require a fresh consent flow when changing permissions.

## Working agreement

- Conversation: Italian. Repository content and runtime text: English. Keep documentation short.
- Follow [AGENTS.md](../../AGENTS.md). No tests, builds, formatters, smoke tests, preflight, CI dispatch or other checks unless the owner explicitly requests them. Say which checks would be useful at handoff.
- Keep tokens and downloaded client configuration out of Git, SQLite metadata, logs and chat. Do not claim completed sign-in or real-account testing before it happens.
- Browser setup and provider registration work can continue independently of implementation.
