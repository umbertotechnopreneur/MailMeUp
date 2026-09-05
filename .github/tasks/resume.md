# Restart checkpoint — 2026-09-05

🟩⬜⬜⬜⬜⬜⬜⬜ **12.5% complete · 87.5% remaining**

**Current phase:** 1 — Prepare the pilot. Paused at the owner's request before a workstation restart.

## Saved state

- The existing .NET 10 foundation remains the implementation baseline. This checkpoint changes documentation only; there are no partial authentication implementations or new package dependencies.
- The executable exposes `get_status` and `list_accounts`. Browser sign-in, protected credentials, mail reads and calendar reads remain to be implemented.
- The [MVP plan](../../docs/MVP_PLAN.md) and [app registration guide](../../docs/APP_REGISTRATION.md) are saved. Foundation validation recorded earlier does not validate future work.
- No Google Cloud project, Google OAuth client or Microsoft app registration was created during the attempted browser session. No account tokens or client credentials were obtained.
- Save this checkpoint as a local commit on `main`. Do not push or trigger CI for this checkpoint.

## Resume here

1. Restore the connection to the owner's authenticated **Edge** browser. The owner has the extension enabled and has authorized browser control to create MailMeUp's project and required registrations.
2. Use the browser extension's explicit Edge selector. Windows Computer Use stopped because it could not establish the current URL; the extension subsequently reported `Browser is not available: edge`. If selection still fails, follow the browser skill's connection troubleshooting before retrying. Do not repeat the blocked Windows automation route.
3. Create a dedicated MailMeUp Google project, enable Gmail and Calendar APIs, configure an external test audience and create a desktop OAuth client. Then prepare the Microsoft desktop registration for personal and work accounts if the required directory is accessible.
4. Continue the already requested implementation with protected credential storage and account setup. Read-only mail and calendar scopes only. Google installed apps require a fresh consent flow when changing permissions; do not assume incremental authorization.

## Working agreement

- Conversation: Italian. Repository content and runtime text: English. Keep documentation short.
- Follow [AGENTS.md](../../AGENTS.md). No tests, builds, formatters, smoke tests, preflight, CI dispatch or other checks unless the owner explicitly requests them. Say which checks would be useful at handoff.
- Keep tokens and downloaded client configuration out of Git, SQLite metadata, logs and chat. Do not claim completed sign-in or real-account testing before it happens.
- Browser setup and provider registration work can resume independently of implementation. A browser restart invalidates old window and tab handles; acquire fresh ones through the supported connection.
