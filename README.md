<p align="center">
  <img src="docs/assets/branding/mailmeup-hero.png" alt="MailMeUp: several inboxes converge into one conversation" width="100%" />
</p>

# MailMeUp

**All your inboxes. One conversation.**

A local, open-source email bridge for Codex and other MCP clients. The goal is to connect multiple Gmail, Google Workspace, Outlook.com and Microsoft 365 accounts, search across them, and retrieve only the messages your conversation needs.

[![CI](https://github.com/umbertotechnopreneur/MailMeUp/actions/workflows/ci.yml/badge.svg)](https://github.com/umbertotechnopreneur/MailMeUp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-71DEB7)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10_LTS-142536)](global.json)

> **Foundation / pre-alpha.** The executable, local metadata store, MCP stdio transport and discovery tools work. Account sign-in, secure token storage and mail operations are planned. You cannot connect or read a real mailbox with this build. The artwork illustrates the planned product.

## Why MailMeUp

- One account registry for personal and work inboxes, with explicit account selection.
- One native executable per operating system and CPU architecture; no separate server to deploy.
- Compact search results first, full text only when needed.
- Credentials protected on the user's device; SQLite reserved for metadata and optional caches.
- MIT-licensed source and GitHub distribution. No marketplace is required.

These are product goals. See the status table for what exists today.

## Current status

| Area | Foundation status |
| --- | --- |
| .NET 10 solution and module boundaries | Implemented |
| CLI help, version, status, account listing | Implemented |
| MCP `get_status` and `list_accounts` | Implemented and tested over stdio |
| SQLite account metadata persistence | Implemented; no account registration command yet |
| Windows, Linux and macOS packaging workflows | Included; six runtime targets |
| Google / Microsoft account sign-in | Planned |
| OS-protected credential storage | Interface only; no plaintext fallback |
| Search, read, threads and mailbox statistics | Planned |
| Sending mail, drafts and attachments | Later, separately scoped milestones |

## Try the foundation

Development requires the [.NET SDK pinned in global.json](global.json). Python 3 is used only for repository checks; end users will not need it.

```sh
dotnet restore MailMeUp.slnx --locked-mode
dotnet build MailMeUp.slnx -c Release --no-restore
dotnet run --project src/MailMeUp.Cli -c Release --no-build -- status
dotnet run --project src/MailMeUp.Cli -c Release --no-build -- accounts list
```

A fresh account list is empty. These commands do not create a database, open a browser or contact a mail provider.

To connect the built server to Codex, replace the example path with the absolute path on your machine:

```sh
codex mcp add mailmeup -- dotnet /absolute/path/MailMeUp/src/MailMeUp.Cli/bin/Release/net10.0/mailmeup.dll --stdio
```

For a published Windows executable:

```powershell
codex mcp add mailmeup -- 'C:\Tools\MailMeUp\mailmeup.exe' --stdio
```

Codex launches the process and exchanges MCP messages over its standard input and output. See [Codex setup](docs/CODEX_SETUP.md) for configuration, verification and removal. Installing the foundation exposes only the two implemented discovery tools.

## The planned flow

![Planned architecture: Google and Microsoft accounts connect to a local MailMeUp process, which exposes tools to Codex](docs/assets/branding/mailmeup-concept.png)

*Concept illustration. Mail content returned through MCP enters the client's conversation and may be processed by its model service. Local credential storage does not mean all email processing is offline.*

## Repository map

```text
src/
  MailMeUp.Core/                  Account models and provider contracts
  MailMeUp.Application/           Shared application facade
  MailMeUp.Storage/               SQLite metadata and data paths
  MailMeUp.Security/              Future protected-credential boundary
  MailMeUp.Providers.Google/      Google module readiness; OAuth/API work pending
  MailMeUp.Providers.Microsoft/   Microsoft module readiness; MSAL/Graph work pending
  MailMeUp.Mcp/                   MCP tools and compact wire output
  MailMeUp.Cli/                   Executable, commands and dependency injection
tests/MailMeUp.Tests/             Metadata integrity and readiness tests
scripts/                         Validation, protocol smoke test and packaging
docs/                            Product, architecture, security and branding
.github/                         CI, release workflow and contribution templates
```

## Documentation

- [Product brief](docs/PRODUCT.md) and [milestones](docs/ROADMAP.md)
- [Architecture](docs/ARCHITECTURE.md) and [MCP contract](docs/MCP_CONTRACT.md)
- [Authentication design](docs/AUTHENTICATION.md) and [privacy](docs/PRIVACY.md)
- [CLI reference](docs/CLI_REFERENCE.md) and [Codex setup](docs/CODEX_SETUP.md)
- [Development and validation](docs/DEVELOPMENT.md) and [releases](docs/RELEASING.md)
- [Brand guide](docs/BRAND.md) and [image prompts/provenance](docs/assets/branding/GENERATION.md)

## Contributing and license

Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. Report vulnerabilities using [SECURITY.md](SECURITY.md). Source is licensed under [MIT](LICENSE); dependencies retain their respective licenses as described in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Created by [Umberto Giacobbi](https://github.com/umbertotechnopreneur), in the same family as [PromptMeUp](https://github.com/umbertotechnopreneur/PromptMeUp). MailMeUp is an independent project and is not endorsed by OpenAI, Google or Microsoft.
