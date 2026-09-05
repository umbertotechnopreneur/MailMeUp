# Validation

Checked locally on Windows on 2026-09-05. Automated checks used synthetic `example.test` data. A real Google Desktop client file was later imported with command output suppressed; no provider account was connected.

## Passed

- Locked dependency restore, formatting check and Release build with zero warnings or errors.
- Sixteen account, storage, provider setup, protected credential, cross-account mail, calendar, pagination and readiness tests.
- Windows protected storage round-trip, deletion and plaintext-file exclusion using a synthetic token.
- Real CLI/MCP process checks for all seven tools, empty first-run reads, invalid references and read-only annotations.
- Dependency inventory, local-data rules and documentation links.
- Google Desktop client validation and import into Windows-protected storage. The source file stayed outside the repository and no credential value was printed.

## Real Google checkpoint

Checked on Windows on 2026-09-05 without printing account identities, provider identifiers, message text, appointment details or credentials:

- Two approved Google test accounts completed installed-app OAuth with PKCE and persisted across new processes. Both report Gmail and Calendar read consent.
- All seven MCP tools advertise read-only behavior.
- Cross-account Gmail search completed with both accounts covered, ten compact results, one bounded detail read and a working continuation cursor.
- Four calendars were listed across both accounts. An empty future primary-calendar window completed normally; a recent all-calendar search returned appointments, read one bounded detail and exercised its continuation cursor.
- A third sign-in was denied by Google because the selected account was not an approved tester. The attempt was cancelled and no third account was saved.

## Real Microsoft and mixed-provider checkpoint

Checked on Windows on 2026-09-05 with the same privacy-preserving output rules:

- Two Microsoft accounts completed interactive sign-in and retained mail/calendar consent across new processes.
- Microsoft mail search covered both accounts with ten compact results, bounded message reading and a working continuation cursor.
- Three Microsoft calendars were listed. A recent appointment was found and read after fixing null optional `location` and `onlineMeeting` values.
- A mixed search covered all four Google/Microsoft accounts. A mixed agenda covered seven calendars, bounded appointment detail and event continuation.
- The focused event parser fix passed the real flow and the full 16-test validation in an isolated clean checkout.

## Manual real-provider check

Live provider checks are deliberately excluded from CI because they require local protected credentials, interactive account access and live provider data. Run `python scripts/real-provider-check.py <path-to-mailmeup>` only on an authorized workstation. The script enumerates every connected account, requires at least two, reads bounded examples when available and prints only privacy-preserving results.

The manual runner passed against the current Windows development executable on 2026-09-05. It enumerated four accounts, read and matched one bounded mail summary/detail example plus continuation per account, listed seven calendars and matched event details for the three accounts that had events in the checked window. The remaining account returned an empty event window normally.

Earlier foundation checks also produced six portable packages and ran CI on Windows, Linux and macOS. The first package rehearsal is recorded in [this successful run](https://github.com/umbertotechnopreneur/MailMeUp/actions/runs/33943689252).

## Limits

The current read-only provider source passes Windows x64 package smoke tests before and after ZIP extraction. Windows ARM64 publishes successfully but cannot run on the x64 host. macOS and Linux are not tested because no machines are available; the current MVP makes no support claim for them.

Known-result comparison, reconnect, revoked access, local removal and a real partial-provider failure remain. Provider scope remains read-only. No release was published.

## Current source changes

Provider setup, account removal, cross-account aggregation and read-only mail/calendar MCP contracts pass local automated checks. Real read flows now pass for two Google and two Microsoft accounts, including mixed-provider search and agenda.

The Spectre.Console presentation, command options, cancellation and Serilog integration pass the local Release build with no warnings, all 16 tests, the CLI/MCP regression script, dependency inventory and repository preflight. The development executable also passes the manual real-provider runner. A distributable package has not been rebuilt for these CLI changes, and visual terminal review remains.
