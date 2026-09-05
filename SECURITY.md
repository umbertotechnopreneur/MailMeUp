# Security policy

MailMeUp is pre-alpha. No production security guarantee or support SLA is offered. The foundation does not authenticate with providers or persist credentials.

Use the repository's **Security → Report a vulnerability** action for a private report when available. If private reporting is unavailable, open an issue requesting a private contact channel without including vulnerability details, credentials or mailbox data. Do not publish exploit details or secrets in an issue.

Reports should identify the affected commit/version, operating system, synthetic reproduction and impact. Review [the authentication design](docs/AUTHENTICATION.md) and [privacy boundaries](docs/PRIVACY.md) before contributing auth or mail code.

Future releases must keep refresh tokens and MSAL cache blobs in OS-protected storage, reject plaintext fallback, isolate accounts, redact diagnostics and keep email content outside the instruction boundary.
