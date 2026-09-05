# Diagnostics

MailMeUp uses Serilog in the executable and `ILogger<T>` in the shared application decorator. Both the CLI and MCP use the same operation diagnostics.

Logs go only to stderr. There is no file sink, telemetry endpoint or log directory created by discovery. The logger is disposed after the host, flushing its sink at shutdown. MCP stdout remains reserved for JSON-RPC; no banner, progress animation or log line is written there.

## Levels

The default is `warning`. Set `MAILMEUP_LOG_LEVEL` or pass `--log-level`; the command option takes precedence. Accepted levels are `verbose`, `debug`, `information`, `warning`, `error` and `fatal`.

```powershell
mailmeup accounts list --json --log-level debug
mailmeup --stdio --log-level information
mailmeup setup status --no-color --no-animation
```

- Debug: operation starts, command lifecycle and bounded startup failure types.
- Information: completed operations with elapsed milliseconds, cancellation and MCP process lifecycle.
- Warning: failed operations and partial account coverage, including only the number of unavailable accounts.

CLI error text is user feedback on stderr and remains visible at any diagnostic log level. Exit status still reports failure. `--no-color`, a nonempty `NO_COLOR`, `TERM=dumb`, redirected stderr and MCP mode disable diagnostic colors.

## Privacy boundary

Only `MailMeUp.*` source categories are admitted to the Serilog sink. External SDK, host and transport logs are excluded at every level because they may include request arguments, response content or provider exception messages. The application decorator supplies bounded diagnostics in their place.

Do not log accounts, addresses, user paths, command arguments, search criteria, local/provider references, message or event content, authorization headers, credentials, or exception objects/messages. Record fixed operation names, counts, durations and exception type names. Keep this rule when adding new application logs; the category filter does not sanitize arbitrary future messages.

Account information explicitly requested by the owner appears in CLI results or MCP responses, separately from diagnostic logging. Terminal metadata is escaped as text, including control and bidirectional-formatting characters.

## Implementation references

- [Serilog host integration](https://github.com/serilog/serilog-extensions-hosting)
- [Serilog console sink](https://github.com/serilog/serilog-sinks-console)
- [Spectre.Console documentation](https://spectreconsole.net/console/)

The current integration has not been built or exercised; see [validation](VALIDATION.md).
