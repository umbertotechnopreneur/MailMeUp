# Accounts and credentials

Provider app setup, interactive multi-account sign-in and protected token caches are present in the current source. Local automated checks pass; real sign-in still needs the provider registrations.

Each user signs in through Google or Microsoft in their browser. Each account is authorized separately. MailMeUp does not need your account password.

## What is stored?

| Item | What it does | Where it belongs |
| --- | --- | --- |
| Client ID | Identifies the application | Public app settings |
| Access token | Gives temporary API access | Memory or protected SDK cache |
| Refresh token | Renews access without another sign-in | Protected operating-system storage |
| Microsoft token cache | Lets MSAL manage account sessions | Protected operating-system storage |
| Account names and addresses | Identify your connected accounts | Local SQLite database |

Tokens must never appear in Codex prompts, logs, GitHub or the SQLite metadata database. Microsoft desktop sign-in does not use a confidential client secret; a distributed executable cannot keep a shared embedded secret confidential.

## Local app setup

- `mailmeup setup google <client-json>` imports a Google **Desktop app** client file. It saves the public client ID in local settings and protects the client secret with the operating system. The downloaded source file remains in place for the user to remove.
- `mailmeup setup microsoft <client-id>` saves the public Application (client) ID. A Microsoft desktop app does not need a client secret.
- `mailmeup setup status` reports whether each provider is configured without revealing credentials.

These commands configure the provider applications. Connect an account afterwards:

- `mailmeup accounts connect google`
- `mailmeup accounts connect microsoft`
- add `--mail-only` or `--calendar-only` to request one data category

Run the command again to add another account. Sign-in uses the system browser and a fresh account choice. Google tokens use a protected slot per account; Microsoft accounts share MSAL's protected multi-account cache without sharing identities.

`mailmeup accounts remove <account-id>` removes local metadata and cached credentials. It does not revoke the grant at Google or Microsoft.

Remove that provider's connected accounts before replacing its app registration. This keeps old token caches from becoming unreachable.

## Read-only access

Request only the permissions needed to read email and calendars. Calendar access requires additional consent; email consent alone is insufficient. Declining calendar access must not disable an existing mail connection.

## Protection and distribution

Use Windows user protection, macOS Keychain or Linux Secret Service. If secure storage is unavailable, stop rather than save tokens as plain text.

Public distribution still requires suitable provider app registrations and any applicable verification. Sharing the program never shares the creator's account tokens.

Developer references: [Google OAuth](https://developers.google.com/identity/protocols/oauth2/native-app), [Calendar permissions](https://developers.google.com/workspace/calendar/api/auth), [MSAL cache](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization).
