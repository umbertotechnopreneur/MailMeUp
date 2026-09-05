# CLI reference

Commands in the current source:

| Command | Behavior |
| --- | --- |
| `mailmeup` / `mailmeup --help` | Help, with terminal styling only when stdout is interactive |
| `mailmeup --version` | Version string |
| `mailmeup status` | JSON describing actual implementation readiness |
| `mailmeup accounts list` | JSON account metadata; empty on a new installation |
| `mailmeup accounts connect <google\|microsoft>` | Open browser sign-in for read-only mail and calendars |
| `mailmeup accounts connect <provider> --mail-only` | Connect with mail read access only |
| `mailmeup accounts connect <provider> --calendar-only` | Connect with calendar read access only |
| `mailmeup accounts remove <account-id>` | Remove local metadata and cached credentials |
| `mailmeup setup status` | Show Google and Microsoft app setup without secrets |
| `mailmeup setup google <client-json>` | Import a Google Desktop app client file into protected local storage |
| `mailmeup setup microsoft <client-id>` | Save a Microsoft desktop app client ID |
| `mailmeup --stdio` | MCP server; stdout contains protocol messages only |

Exit codes: `0` success, `1` known operational/configuration failure, `2` unknown command. Diagnostics go to stderr. Unexpected programming faults may use the runtime's own nonzero exit code.

`MAILMEUP_DATA_DIR` selects an absolute data directory. By default the program uses .NET's per-user `LocalApplicationData` directory plus `MailMeUp`, independent of the working directory. `accounts.db` is created only when metadata is first saved, not during discovery.

Provider setup does not sign in an account. Complete it before running `accounts connect`. Run `accounts list` to obtain the local ID used by `accounts remove`. Removal does not revoke access at the provider.

The Google source file is not deleted automatically. Store it privately and remove it when no longer needed. These commands pass local automated checks; interactive sign-in still needs real provider registrations.
