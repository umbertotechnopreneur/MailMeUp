# MailMeUp — portable read-only MVP build

All your inboxes. One conversation.

This pre-alpha build connects multiple Google and Microsoft accounts, searches mail and shows a combined calendar agenda through an MCP stdio endpoint. Account and provider setup remains a local CLI action.

Current scope is read-only: no sending mail, changing or deleting provider data, creating appointments or sending invitations.

Run `mailmeup --help`, `mailmeup setup status` or `mailmeup accounts list` (use `mailmeup.exe` on Windows). Register the Google and Microsoft desktop apps first, then use `mailmeup accounts connect <google|microsoft>`. A new installation has no accounts.

Client configuration and token caches use the operating system credential store. Linux requires a working Secret Service. A self-contained build does not require the .NET SDK, but still requires a compatible OS and its standard .NET native dependencies.

Register the executable using an absolute path:

```text
codex mcp add mailmeup -- /absolute/path/mailmeup --stdio
```

Use the Windows executable path when applicable. Codex launches and controls the process; do not run a second server manually. The available tools are `get_status`, `list_accounts`, `search_mail`, `read_mail`, `list_calendars`, `search_events` and `read_event`.

`MAILMEUP_DATA_DIR` optionally selects an absolute private data directory. No tokens or real mailbox data are included. Native library extraction requires a writable local location. See BUILD_INFO.txt for version, commit and whether this archive received a native smoke test. These pre-alpha binaries are unsigned.

Registration and setup guide: https://github.com/umbertotechnopreneur/MailMeUp/blob/main/docs/APP_REGISTRATION.md

Documentation and source: https://github.com/umbertotechnopreneur/MailMeUp

Copyright (c) 2026 Umberto Giacobbi. MIT license; dependency licenses are included separately.
