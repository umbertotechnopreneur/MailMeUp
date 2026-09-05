# Connect to Codex

MailMeUp is a local MCP program. No marketplace is required.

**The foundation exposes status and account listing only. Real email and calendar access is still planned, with read-only permissions.**

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

An empty list is expected today. Future mailbox sign-in will happen through MailMeUp, not `codex mcp login`.

Remove the registration with `codex mcp remove mailmeup`. This does not delete data or revoke provider access.

Reference: [OpenAI MCP setup](https://developers.openai.com/codex/mcp/), checked alongside the local CLI on 2026-09-05.
