# MailMeUp — portable foundation build

All your inboxes. One conversation.

This pre-alpha build provides CLI discovery and an MCP stdio endpoint. Google/Microsoft sign-in, protected token storage, mail and calendar operations are not implemented yet.

Current scope is read-only: no sending mail, changing or deleting provider data, creating appointments or sending invitations.

Run `mailmeup --help`, `mailmeup status` or `mailmeup accounts list` (use `mailmeup.exe` on Windows). A new installation has no accounts. A self-contained build does not require the .NET SDK, but still requires a compatible OS and its standard .NET native dependencies.

Register the executable using an absolute path:

```text
codex mcp add mailmeup -- /absolute/path/mailmeup --stdio
```

Use the Windows executable path when applicable. Codex launches and controls the process; do not run a second server manually. The available tools are `get_status` and `list_accounts`.

`MAILMEUP_DATA_DIR` optionally selects an absolute private data directory. No tokens or real mailbox data are included. Native library extraction requires a writable local location. See BUILD_INFO.txt for version, commit and whether this archive received a native smoke test. The foundation binaries are unsigned.

Documentation and source: https://github.com/umbertotechnopreneur/MailMeUp

Copyright (c) 2026 Umberto Giacobbi. MIT license; dependency licenses are included separately.
