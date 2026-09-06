# Active work

- Windows MSIX `0.1.1.3` is locally installed; the About/banner UI was inspected and 111 .NET tests plus published/alias MCP smoke checks passed. Validate clean-machine deployment, additional DPI settings, Google/Microsoft UI sign-in and local Codex plugin installation next.
- Exercise simultaneous installed-alias callers and an MSIX upgrade without changing the Codex command; redirected MCP stdio already passed on the installed alias.
- Synthetic sharing and caller-notification regressions passed, including cached references and in-flight reads. Validate the corresponding live account scenarios only when requested.
- Preserve the package identity and signing publisher across upgrades; do not publish or install on the owner's machine without a request.

- The earlier Windows CLI source passed 68 .NET tests, 24 manual-runner regressions and 27 real-provider checks; three event checks were skipped. These historical live checks are separate from the current synthetic/UI validation. See [validation](../../docs/VALIDATION.md).
- Windows x64 package `de1fae7` passed native and extracted smoke checks, live reads and preservation of all four accounts. Clean installation remains.
- Compare known mail and calendar examples independently, including recurrence, cancellation, all-day dates and time zones.
- Deliberately check real expiry, revoked access, reconnect and lost connectivity when requested. Existing synthetic fault tests do not validate those live paths.
- Check installation on a clean Windows machine and start a small pilot. Windows ARM64 is build-only; macOS/Linux runtime testing is outside the current MVP.
- Keep email and calendar access strictly read-only. Provider writes need a separate explicit decision.
- Validate the new unread/date-range mail tools, structured filters and Spam/Junk plus Trash/Deleted exclusions against synthetic and real provider cases.
- Maintain the progress plan in the versioned [docs/MVP_PLAN.md](../../docs/MVP_PLAN.md); do not maintain separate output copies.
