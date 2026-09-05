# Product brief

**MailMeUp — All your inboxes. One conversation.**

MailMeUp will let an individual connect multiple personal and work email accounts to one local executable and ask an MCP client to work across them. A shared repository distributes the program; each installation owns its own account authorizations and storage.

## Initial user journey

1. Download the executable for the user's OS/architecture.
2. Register provider application settings or use an approved distribution's public client registration.
3. Authorize each account through the provider's browser login.
4. Register `mailmeup --stdio` in Codex or another MCP client.
5. Ask for a search, inspect compact matches and read selected messages.

Steps 2, 3 and 5 require later milestones. The foundation supports installation testing and capability discovery only.

## First usable milestone

Read-only access to Gmail/Workspace and Outlook.com/Microsoft 365, several accounts per provider, explicit account scoping, compact cross-account search, selective plain-text reads and useful partial-failure reports.

## Out of scope initially

Sending mail, attachments, mailbox mutation, background full-mail synchronization, shared/team credentials, a web dashboard, hosted MCP, billing and marketplace submission.

## Product principles

- Make the selected accounts visible and distinguish account scope from folder scope.
- Report actual coverage and failures; one unavailable mailbox must not become an empty successful search.
- Spend the conversation's context on relevant content. Search previews precede body retrieval.
- Keep authentication and secrets outside model prompts and MCP results.
- Require no continuously running background process: the MCP client owns the child process.
