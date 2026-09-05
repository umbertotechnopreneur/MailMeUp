# Privacy

The foundation does not connect to Google, Microsoft or an AI service. It has no analytics integration and stores no real mail or calendar content.

## Read-only scope

Future provider access is limited to reading and searching. MailMeUp will not send messages, modify provider data, delete messages or appointments, or send invitations.

Local setup and caching still need local storage.

## Local does not mean offline

Credentials stay on your computer in protected storage. Information requested through MailMeUp is returned to the assistant, which may send it to its AI service.

Email text, event titles, attendees and meeting links can all be sensitive. Return only what is needed.

## Storage

SQLite is for account metadata and optional future caches, not tokens. The foundation database is not encrypted; use a private user directory. No message or calendar cache exists yet.

Logs must exclude message content, meeting details and credentials. Future account removal must explain the difference between deleting local data and revoking access at the provider.
