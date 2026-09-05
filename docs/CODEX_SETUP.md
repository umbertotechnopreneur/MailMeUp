# Connect to Codex

MailMeUp is a local MCP program. No marketplace is required.

**The current source exposes seven read-only MCP tools and passes local automated checks. Account setup remains a local CLI action; real provider sign-in still needs the app registrations and pilot accounts.**

## 1. Build or extract

Developers can follow [the build guide](DEVELOPMENT.md). For portable packages, extract the matching OS/CPU archive into a stable folder.

## 2. Register the program

Windows example:

```powershell
codex mcp add mailmeup -- 'C:\Tools\MailMeUp\mailmeup.exe' --stdio
```

Linux/macOS example:

```sh
codex mcp add mailmeup -- /absolute/path/mailmeup --stdio
```

Local source-build example:

```powershell
codex mcp add mailmeup -- dotnet 'E:\MailMeUp\src\MailMeUp.Cli\bin\Release\net10.0\mailmeup.dll' --stdio
```

Use your own absolute path. Codex starts the process; you do not start another server manually.

## 3. Check it

Run `codex mcp list`. Reload the Codex task/app if necessary, then ask:

> Use MailMeUp to show its status and list configured accounts.

An empty list is expected before local setup. Mailbox sign-in happens through `mailmeup accounts connect`, not `codex mcp login`. Real-provider checks remain pending.

Remove the registration with `codex mcp remove mailmeup`. This does not delete data or revoke provider access.

Reference: [OpenAI MCP setup](https://developers.openai.com/codex/mcp/), checked alongside the local CLI on 2026-09-05.
