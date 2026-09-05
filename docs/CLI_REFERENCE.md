# CLI reference

Commands supported by the foundation:

| Command | Behavior |
| --- | --- |
| `mailmeup` / `mailmeup --help` | Help, with terminal styling only when stdout is interactive |
| `mailmeup --version` | Version string |
| `mailmeup status` | JSON describing actual implementation readiness |
| `mailmeup accounts list` | JSON account metadata; empty on a new installation |
| `mailmeup --stdio` | MCP server; stdout contains protocol messages only |

Exit codes: `0` success, `1` known operational/configuration failure, `2` unknown command. Diagnostics go to stderr. Unexpected programming faults may use the runtime's own nonzero exit code.

`MAILMEUP_DATA_DIR` selects an absolute data directory. By default the program uses .NET's per-user `LocalApplicationData` directory plus `MailMeUp`, independent of the working directory. `accounts.db` is created only when metadata is first saved, not during discovery.

Account connection/removal commands are planned. Do not manually insert database rows and treat them as authenticated accounts.
