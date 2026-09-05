# Connect MailMeUp to Codex

This project ships a standalone MCP executable. No marketplace account or plugin bundle is necessary. The foundation exposes only `get_status` and `list_accounts`; email tools appear only after later milestones.

## Register a published executable

Extract the archive for your operating system and CPU into a stable directory you control. Do not register a path inside a temporary download/extraction directory.

Windows:

```powershell
codex mcp add mailmeup -- 'C:\Tools\MailMeUp\mailmeup.exe' --stdio
```

Linux/macOS:

```sh
codex mcp add mailmeup -- /absolute/path/mailmeup --stdio
```

For local source development, build first and register the DLL instead:

```powershell
codex mcp add mailmeup -- dotnet 'E:\MailMeUp\src\MailMeUp.Cli\bin\Release\net10.0\mailmeup.dll' --stdio
```

## Equivalent TOML

Add this to the Codex configuration file used by your installation (normally `~/.codex/config.toml`), adapting the absolute path:

```toml
[mcp_servers.mailmeup]
command = 'C:\Tools\MailMeUp\mailmeup.exe'
args = ["--stdio"]
```

An optional `[mcp_servers.mailmeup.env]` table can set `MAILMEUP_DATA_DIR` to an absolute private path. Do not put provider tokens or passwords in Codex configuration.

## Verify

Run `codex mcp list` and `codex mcp get mailmeup`. Reload the Codex task/app if the current session has not refreshed its tool inventory. Ask: **“Use MailMeUp to show its status and list configured accounts.”** An empty account list is expected until authentication is implemented.

Codex launches and owns the child process. You do not need to start a second copy manually. There is no URL, port or MCP bearer token to configure for stdio. `codex mcp login` is not the future Google/Microsoft mailbox sign-in mechanism; that will be a MailMeUp account setup command.

Remove the registration with `codex mcp remove mailmeup`. This changes client configuration; it does not delete application data or revoke provider grants.

## Troubleshooting

Check the absolute executable path, executable permissions on Unix, and architecture. A framework-dependent DLL needs the .NET 10 runtime; a self-contained release includes it. Never launch through a shell wrapper that prints a banner on stdout. Native library extraction may need a writable per-user temporary directory.

The configuration syntax was checked against the local Codex CLI and [OpenAI's MCP documentation](https://developers.openai.com/codex/mcp/) on 2026-09-05. Client UI labels may vary by version.
