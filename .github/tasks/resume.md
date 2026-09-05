# Restart checkpoint — 2026-09-05

🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% verified · 15/24 checkpoints**

**Current phase:** Windows packaging and update preservation.

## Saved state

- Pre-alpha and strictly read-only. No additional OAuth scopes, message/event writes, tags or releases.
- Current source passed 68 .NET tests, 24 isolated manual-runner tests, CLI/MCP smoke checks and a clean Release build.
- Live reads passed 27 checks across two Google and two Microsoft accounts and seven calendars; three event checks were skipped. Reports contain no identities or private content.
- Failed reconnect, cross-process credential locking, partial results, bounded pagination and calendar boundaries have synthetic regression coverage.
- Real deliberate revocation/reconnect and independent comparison with provider screens remain. No real accounts were removed to simulate faults.
- The existing Windows archive is from `971a5b8`; package the newly committed source next.

## Resume here

1. Package Windows x64 from a clean commit, test the published executable and compare account metadata with the previous version.
2. Check installation on a clean Windows machine and prepare the owner pilot.
3. Compare real all-day, time-zone, recurring and cancelled examples independently.
4. Deliberately check real expiry, revoked access, reconnect and connectivity failures when requested.

Windows ARM64 remains unexecuted on hardware; macOS/Linux runtime flows are not tested because no machines are available.

See the [MVP plan](../../docs/MVP_PLAN.md), [validation record](../../docs/VALIDATION.md) and [account recovery guide](../../docs/RECOVERY.md).

## Working agreement

- Conversation: Italian. Repository content and runtime text: English. Keep documentation short.
- Follow [AGENTS.md](../../AGENTS.md). Run tests, builds and other checks only when explicitly requested by the owner.
- Keep tokens and downloaded client configuration out of Git, SQLite metadata, logs and chat.
