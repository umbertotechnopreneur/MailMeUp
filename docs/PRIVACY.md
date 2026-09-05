# Privacy and data boundaries

The foundation does not contact Google, Microsoft or an LLM service. It contains no analytics or telemetry integration. It can read account metadata from a local SQLite registry; first-run status and account discovery create no files.

## Planned data flow

Provider credentials stay in OS-protected local storage and are used by the local process to call provider APIs. Headers and message text requested through MCP are returned to the client. The client may include them in prompts sent to its model service according to that client's configuration and terms. **Local credential storage is not a promise that email content never leaves the device.**

## Storage and logs

The database is not encrypted by this foundation. Account labels and email addresses are personal data even though they are not credentials. Use a private per-user directory. Unix directory creation requests owner-only permissions; an existing directory's permissions are not rewritten. Windows uses the selected directory's inherited permissions. Shared or synchronized directories are not an appropriate credential location.

No message cache exists yet. A future cache needs explicit retention, clear-cache behavior and documented fields. Do not log mail bodies, subjects, access/refresh tokens, MSAL cache contents or authorization headers. Diagnostic errors exposed by the CLI omit exception messages that may contain paths or account data.

Future account removal must distinguish deleting local credentials and cached metadata from revoking the provider grant. Document both operations; local deletion alone cannot promise provider-side revocation.
