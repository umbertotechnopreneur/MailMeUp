# Active work

- Complete the provider registrations described in `docs/MVP_PLAN.md`; the Google Desktop client is configured locally, while its cloud-side settings still need confirmation and the Microsoft registration remains pending.
- Local Release build, 16 tests and the seven-tool MCP smoke test pass. Windows protected storage passes; real providers remain.
- Next milestone: connect one Google test account with the six read-only scopes, then validate phases 2–5 with real known-result examples.
- Windows x64 packaging passes before and after extraction; Windows ARM64 publishes but is unexecuted. macOS/Linux testing is outside the current MVP because no machines are available.
- Keep email and calendar access strictly read-only. Provider writes need a separate explicit decision.
