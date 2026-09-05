# Active work

- The Spectre.Console presentation and Serilog diagnostics pass the local build, 16 tests, CLI/stdio regression checks and privacy-preserving live reads. A new distributable package and visual terminal review remain.
- Two Google and two Microsoft accounts now pass privacy-preserving read-only mail/calendar checks, including mixed four-account searches.
- The focused Microsoft null-event-field fix passes real appointment reading and the 16-test suite in an isolated checkout. The separate CLI/logging work still has its own validation state.
- A manual runner enumerates every connected account, requires at least two and checks bounded examples without joining the CI workflow.
- Next milestone: check reconnect, revoked access, local removal and partial-provider failure reporting.
- Windows x64 packaging passes before and after extraction; Windows ARM64 publishes but is unexecuted. macOS/Linux testing is outside the current MVP because no machines are available.
- Keep email and calendar access strictly read-only. Provider writes need a separate explicit decision.
