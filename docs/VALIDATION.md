# Validation

**Windows x64, 2026-09-05. Pre-alpha and read-only.** Local automated checks use synthetic `example.test` data and isolated storage. Authorized live reads use local protected credentials; reports contain counts and outcomes only.

## Current local results

- Locked restore, formatting and Release build passed with zero warnings or errors.
- **68 .NET tests passed:** storage, account isolation, failed reconnect, selective removal, transactional MSAL cache updates, credential locking/cancellation, partial reads, continuation limits and calendar boundaries.
- Windows protected credential I/O and separate-process lock contention passed with synthetic data. Timeout tests cover cancellation classification; they do not wait for the actual 30-second deadline.
- **24 manual-runner regression tests passed**, without starting MailMeUp or accessing real accounts. They cover CI refusal, 0/1-account rejection, dynamic 2/3/4-account runs, individual failures, missing consent, empty calendars, batching and sanitized output.
- CLI/MCP process checks passed for all seven read-only tools, empty first-run reads, invalid references, JSON and private diagnostics on stderr.
- Dependency inventory and repository data/link checks passed.

## Current real-provider results

Both the development executable and the published Windows x64 executable from `de1fae7` passed **27 checks, with 0 failures and 3 skips** per run, across **two Google and two Microsoft accounts**:

- Four bounded mail summary/detail samples and mail continuation reads.
- Seven calendars, mixed-provider searches and three bounded event summary/detail samples.
- One Microsoft account had no events in the checked window: its event detail and continuation checks were skipped. Another had no next event page, so that continuation check was skipped.
- All four accounts remained available across new processes. No identities, provider IDs, message/event content or credentials were printed.

These checks compare summaries with fetched details. They do **not** independently compare results with Gmail, Outlook or calendar UI screens. No real account was removed or its grant revoked to simulate faults.

The manual runner is local-only: `python scripts/real-provider-check.py <path-to-mailmeup>`. It enumerates accounts, requires at least two and refuses recognized CI environments before process startup. Live checks are excluded from CI.

At the owner's request, CI also skips unit-test execution. It retains build, formatting, isolated protocol smoke and repository checks. Local `scripts/validate.ps1` runs the unit suite unless `-SkipUnitTests` is supplied.

## Packaging and CI

The Windows x64 package from `de1fae7` passed native smoke checks before and after ZIP extraction, then the real-provider checks above. Account metadata matched exactly between the previous `971a5b8` executable and this build: all four accounts were preserved and usable. Diagnostics checks used synthetic data and printed no credentials or private content. This was an update on the development workstation; a clean Windows installation remains untested.

Earlier foundation CI produced six portable packages; see [the recorded run](https://github.com/umbertotechnopreneur/MailMeUp/actions/runs/33943689252). That historical run does not validate the current source. Current-turn results above are local, not remote CI results.

Windows ARM64 was published previously but not executed on hardware. **Real macOS and Linux flows are not tested:** no machines are available, and the current MVP makes no support claim for them.

## Still to check

- Deliberate real expiry, revoked access, reconnect and lost connectivity.
- Independent known-result comparison, including recurring/cancelled events, all-day dates and time zones.
- Clean Windows installation and independent pilot use.

Google remains External in Testing. No provider-write scopes, releases or release tags were added.
