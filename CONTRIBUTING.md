# Contributing

MailMeUp currently welcomes focused foundation fixes and discussion of the next milestone. Start with the [roadmap](docs/ROADMAP.md) and open an issue before implementing a new provider or changing the security model.

1. Fork the repository and create a focused branch.
2. Use .NET 10 from `global.json`, Python 3.10+ and PowerShell 7 for the validation scripts.
3. Keep source, comments and documentation in English. Use synthetic mail data only.
4. Run `pwsh -NoProfile -File scripts/validate.ps1`.
5. Open a pull request describing the user-visible behavior, validation and limitations.

Do not include credentials, mailbox contents, token caches, local file paths from another user's machine or private provider configuration in issues or pull requests. New authentication code needs tests for account isolation, refresh races, revocation and unavailable credential storage.

Contributions are made under the repository's MIT license. Generated artwork should include its source prompt and provenance; do not imply third-party endorsement.
