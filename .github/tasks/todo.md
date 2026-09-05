# Active work

- Current Windows source passed 68 .NET tests, 24 manual-runner regressions and 27 real-provider checks; three event checks were skipped. See [validation](../../docs/VALIDATION.md).
- Package the committed Windows x64 executable, repeat live reads and compare account metadata with the previous package.
- Compare known mail and calendar examples independently, including recurrence, cancellation, all-day dates and time zones.
- Deliberately check real expiry, revoked access, reconnect and lost connectivity when requested. Existing synthetic fault tests do not validate those live paths.
- Check installation on a clean Windows machine and start a small pilot. Windows ARM64 is build-only; macOS/Linux runtime testing is outside the current MVP.
- Keep email and calendar access strictly read-only. Provider writes need a separate explicit decision.
