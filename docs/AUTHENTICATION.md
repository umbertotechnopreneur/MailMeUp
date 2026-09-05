# Authentication and credential design

**Planned design. No OAuth flow or credential adapter is implemented in the foundation.**

## What is a credential?

| Item | Meaning | Planned handling |
| --- | --- | --- |
| OAuth client ID | Identifies the installed application | Public configuration; safe to distribute when registration permits |
| Access token | Short-lived bearer authorization to a provider API | Memory; if included in an SDK cache, protect the whole cache |
| Refresh token | Allows obtaining new access tokens without another browser login | OS-protected credential storage; never SQLite or MCP output |
| MSAL cache blob | SDK-managed account and token state, including sensitive tokens | Protected MSAL cache persistence; do not extract/reimplement refresh logic |
| ID token | Identity assertion, not authorization to read mail | Validate through the identity library; do not use as a mail API bearer token |
| Client secret | Confidential-client credential | Microsoft desktop public-client flows do not use one; native binaries cannot keep a shared secret confidential |

If Google's desktop registration supplies a client-secret field, follow its installed-app requirements and treat the app as a public native client. A distributed executable cannot make that field a confidential security boundary. Do not embed a confidential web-app credential in the binary.

## Google

Use a desktop OAuth application, system-browser authorization code flow, PKCE, state validation and a loopback redirect with a short-lived listener. Authorize each mailbox separately. Request only the scopes needed for the read-only milestone; Gmail read scopes and external distribution may trigger consent-screen and verification requirements.

Store the verified Google identity separately from the user-visible email alias. Protect refresh tokens, coordinate refreshes and preserve existing refresh material if a later response omits it. Handle revoked grants with an explicit reconnect status.

## Microsoft

Use an Entra application configured for the intended personal and organizational account audience, MSAL public-client flows and delegated Microsoft Graph permissions. Prefer the library's supported interactive/silent acquisition and protected cache integration. Organizational consent policies can still block an account.

Persist and select the full MSAL account identity/tenant context rather than assuming email uniquely identifies an account. SDK cache callbacks need cross-process synchronization. Do not implement a parallel refresh-token cache beside MSAL.

## OS protection

- Windows: user-scoped DPAPI-protected storage or an appropriate supported credential store.
- macOS: Keychain-backed protection with explicit application/service identity.
- Linux desktop: Secret Service/libsecret through a supported adapter.

The implementation must detect locked or missing secure storage and stop with a useful error. No automatic plaintext fallback. Headless environments without a vault are outside the initial supported auth matrix. SQLite will store account metadata and opaque credential references, never the credential values.

OS protection protects persisted credentials; it does not defend against arbitrary malicious software already running as the same user. Stdio tools run with the account and permissions of the launching user.

## Distribution decision before milestone 4

Development can use each contributor's own provider registrations. A public release must decide whether to supply maintainer-managed public client IDs or require users to bring their own. Shipping an MIT executable does not itself satisfy provider registration, consent, verification or tenant-policy requirements. The creator's account tokens must never be distributed with the program.

## Sources

Design checked on 2026-09-05 against [Google installed-app OAuth](https://developers.google.com/identity/protocols/oauth2/native-app), [Google OAuth policies](https://developers.google.com/identity/protocols/oauth2/policies) and [MSAL token cache serialization](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization). Recheck scope and public-distribution requirements when implementing the corresponding milestone.
