# Restart checkpoint — 2026-09-05

🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩⬜⬜⬜⬜⬜⬜⬜ **12.5% verified · 87.5% awaiting verification or later work**

**Current phase:** provider registrations and real-account verification for phases 2–5. Edge extension communication is being repaired separately.

## Saved state

- The MCP source exposes discovery plus `search_mail`, `read_mail`, `list_calendars`, `search_events` and `read_event`. Local CLI source adds provider setup, account connection, scope selection and local removal.
- Public provider IDs are stored in `provider-settings.json`. Google client secrets and per-account tokens use protected slots; Microsoft uses a protected MSAL multi-account cache. SQLite stores non-secret account metadata and read categories.
- Provider reads use short in-memory references, bounded text and coverage reporting. Release build, 16 tests, Windows protected storage and the seven-tool MCP smoke test pass locally.
- The [MVP plan](../../docs/MVP_PLAN.md) and [app registration guide](../../docs/APP_REGISTRATION.md) are saved. Foundation validation recorded earlier does not validate future work.
- The Google Cloud project `mailmeup` exists (project number `669810524015`). Its Desktop client is imported and protected. Two approved test accounts complete PKCE sign-in and real Gmail/Calendar reads; a non-approved account is denied in Testing mode. Microsoft registration remains pending.
- The privacy-preserving Google checkpoint covered ten compact mail results, bounded message reading, mail pagination, four calendars, recent appointment search, bounded appointment reading and event pagination. The future primary-calendar window was empty and completed normally.
- The provider setup/sign-in/read source, tests and dependency locks are committed locally. Real provider and clean Windows x64 checks remain.

## Resume here

1. Compare a known Google message and upcoming appointment with the provider UI, including time zone and recurrence behavior.
2. Check Google reconnect, revoked access and local removal without changing provider data.
3. Create the Microsoft public desktop registration for personal and work accounts and record its Application (client) ID.
4. Connect one authorized Microsoft test account at a time and repeat the mail/calendar flow.
5. Keep all provider operations read-only. Google installed apps require a fresh consent flow when changing permissions.

## Working agreement

- Conversation: Italian. Repository content and runtime text: English. Keep documentation short.
- Follow [AGENTS.md](../../AGENTS.md). No tests, builds, formatters, smoke tests, preflight, CI dispatch or other checks unless the owner explicitly requests them. Say which checks would be useful at handoff.
- Keep tokens and downloaded client configuration out of Git, SQLite metadata, logs and chat. Do not claim completed sign-in or real-account testing before it happens.
- Browser setup and provider registration work can continue independently of implementation.
