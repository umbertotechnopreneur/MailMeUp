# Getting started

MailMeUp is a local, read-only MCP program. The current tested target is Windows x64. Keep the extracted folder in a stable private location.

## 1. Register the provider apps

Follow the short [Google and Microsoft registration guide](APP_REGISTRATION.md). Keep the Google JSON file private. Microsoft supplies a public Application (client) ID.

## 2. Configure MailMeUp

Windows example:

```powershell
.\mailmeup.exe setup google 'C:\Private\client_secret.json'
.\mailmeup.exe setup microsoft '<application-client-id>'
.\mailmeup.exe setup status
```

macOS and Linux are not tested because no test machines are available, so the current MVP does not claim support for them. Windows ARM64 builds but has not been executed on ARM64 hardware.

## 3. Connect accounts

Run the command once for each account:

```powershell
.\mailmeup.exe accounts connect google
.\mailmeup.exe accounts connect microsoft
.\mailmeup.exe accounts list
```

Add `--mail-only` or `--calendar-only` when you want only one read category.

## 4. Add it to Codex

```powershell
codex mcp add mailmeup -- 'C:\Tools\MailMeUp\mailmeup.exe' --stdio
codex mcp list
```

Restart or reload Codex if the new MCP server is not visible. Then try:

> Use MailMeUp to search all my connected inboxes for the quarterly plan.

> Use MailMeUp to show appointments from all my connected calendars for the next seven days.

MailMeUp returns short results first and reads details only when requested. It cannot send mail, change messages, edit appointments or send invitations.

Remove a local account with `mailmeup accounts remove <account-id>`. This removes local metadata and cached credentials; provider access can be revoked separately in Google or Microsoft account settings.
