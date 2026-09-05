# Privacy

The current source can sign in and read selected mail/calendar data from providers. It does not call an AI service directly, has no analytics integration and does not cache mail or calendar content on disk.

## Read-only scope

Provider access is limited to reading and searching. MailMeUp will not send messages, modify provider data, delete messages or appointments, or send invitations.

Local provider setup uses a small settings file and operating-system protected credential storage.

## Local does not mean offline

Credentials stay on your computer in protected storage. Information requested through MailMeUp is returned to the assistant, which may send it to its AI service. Short result references and pagination state remain in memory for about 30 minutes.

Email text, event titles, attendees and meeting links can all be sensitive. Return only what is needed.

## Storage

SQLite is for account identity, granted read categories and optional future caches, not tokens or provider app secrets. The database is not encrypted; use a private user directory. No message or calendar cache exists yet.

Logs exclude message content, meeting details, provider responses and credentials. Account removal deletes local metadata and cached credentials; revoking the provider grant remains a separate action in Google or Microsoft account settings.
