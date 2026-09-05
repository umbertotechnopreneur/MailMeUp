# Accounts and credentials

**Planned. Sign-in and protected token storage are not implemented yet.**

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

## Read-only access

Request only the permissions needed to read email and calendars. Calendar access requires additional consent; email consent alone is insufficient. Declining calendar access must not disable an existing mail connection.

## Protection and distribution

Use Windows user protection, macOS Keychain or Linux Secret Service. If secure storage is unavailable, stop rather than save tokens as plain text.

Public distribution still requires suitable provider app registrations and any applicable verification. Sharing the program never shares the creator's account tokens.

Developer references: [Google OAuth](https://developers.google.com/identity/protocols/oauth2/native-app), [Calendar permissions](https://developers.google.com/workspace/calendar/api/auth), [MSAL cache](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization).
