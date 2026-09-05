# Connect to Codex

MailMeUp is a local MCP program. No marketplace is required.

**The current source exposes nine read-only MCP tools. Account setup remains a local CLI action; real provider sign-in still needs the app registrations and pilot accounts.**

## 1. Build or extract

Developers can follow [the build guide](DEVELOPMENT.md). The current MVP target is Windows x64; extract its ZIP into a stable folder.

## 2. Register the program

Windows example:

```powershell
codex mcp add mailmeup -- 'C:\Tools\MailMeUp\mailmeup.exe' --stdio
```

Linux/macOS reference only — these platforms are not tested or supported by the current MVP:

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

For common mailbox requests, prefer `search_unread_mail` for unread messages and `search_mail_by_date` for a received-time range. Both exclude Spam/Junk and Trash/Deleted Items by default; omit `account_ids` to include all mail-enabled accounts.

Remove the registration with `codex mcp remove mailmeup`. This does not delete data or revoke provider access.

Reference: [OpenAI MCP setup](https://developers.openai.com/codex/mcp/), checked alongside the local CLI on 2026-09-05.
