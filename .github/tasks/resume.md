# Restart checkpoint — 2026-09-05

🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩🟩🟩🟩🟩🟨⬜⬜ **66.7% verified · 16/24 checkpoints**

**Current phase:** clean Windows installation and preparation for the owner pilot.

## Saved state

- Pre-alpha and strictly read-only. No additional OAuth scopes, message/event writes, tags or releases.
- Current source passed 68 .NET tests, 24 isolated manual-runner tests, CLI/MCP smoke checks and a clean Release build.
- Live reads passed 27 checks across two Google and two Microsoft accounts and seven calendars; three event checks were skipped. Reports contain no identities or private content.
- Failed reconnect, cross-process credential locking, partial results, bounded pagination and calendar boundaries have synthetic regression coverage.
- Real deliberate revocation/reconnect and independent comparison with provider screens remain. No real accounts were removed to simulate faults.
- Windows x64 archive `artifacts/mailmeup-0.1.0-alpha.1-win-x64.zip` comes from source commit `de1fae7` and passed native/extracted smoke checks and live reads. Updating from `971a5b8` preserved all four accounts and their metadata.
- The plan is versioned in `docs/MVP_PLAN.md`. External output copies are retired; maintain the repository file only.

## Resume here

1. Check installation on a clean Windows machine and prepare the owner pilot.
2. Compare real all-day, time-zone, recurring and cancelled examples independently.
3. Deliberately check real expiry, revoked access, reconnect and connectivity failures when requested.

Windows ARM64 remains unexecuted on hardware; macOS/Linux runtime flows are not tested because no machines are available.

See the [MVP plan](../../docs/MVP_PLAN.md), [validation record](../../docs/VALIDATION.md) and [account recovery guide](../../docs/RECOVERY.md).

## Working agreement

- Conversation: Italian. Repository content and runtime text: English. Keep documentation short.
- Follow [AGENTS.md](../../AGENTS.md). Run tests, builds and other checks only when explicitly requested by the owner.
- Keep tokens and downloaded client configuration out of Git, SQLite metadata, logs and chat.
