<p align="center">
  <img src="docs/assets/branding/mailmeup-hero.png" alt="MailMeUp: several inboxes, one conversation" width="100%" />
</p>

# MailMeUp

**All your inboxes. One conversation.**

Bring your email and appointments from multiple Google and Microsoft accounts into Codex or another compatible AI assistant.

[![CI](https://github.com/umbertotechnopreneur/MailMeUp/actions/workflows/ci.yml/badge.svg)](https://github.com/umbertotechnopreneur/MailMeUp/actions/workflows/ci.yml)
[![MIT](https://img.shields.io/badge/license-MIT-71DEB7)](LICENSE)

> **Read-only by design for the current scope.** MailMeUp will help you find and read information. It will not send email, change messages, create or edit appointments, delete anything from your accounts, or send invitations.

## What it will do

- Search across Gmail, Google Workspace, Outlook.com and Microsoft 365 inboxes.
- Show appointments from Google Calendar and Microsoft calendars.
- Let you choose which accounts and calendars to include.
- Return short results first, then open the details you need.

MailMeUp runs on your computer. Each user connects their own accounts. No marketplace or hosted service is required.

## What works today

**This is a foundation build, not a ready-to-use email or calendar integration.**

The executable starts, reports its status and exposes two discovery tools: `get_status` and `list_accounts`. The project includes documentation, automated checks and packaging for Windows, Linux and macOS.

**Account sign-in, protected token storage, email searches and calendar access are still to be built.**

## How it fits together

![Planned email workflow: accounts connect to MailMeUp on your device, then to Codex](docs/assets/branding/mailmeup-concept.png)

*Concept artwork for the email workflow. Calendars are also in scope. Information requested by your assistant may be sent to its AI service; running locally does not mean all processing is offline.*

## Explore the project

- [Short product overview](docs/PRODUCT.md)
- [Roadmap](docs/ROADMAP.md)
- [Calendars and appointments](docs/CALENDARS.md)
- [Accounts and credentials](docs/AUTHENTICATION.md)
- [Privacy](docs/PRIVACY.md)
- [Connect to Codex](docs/CODEX_SETUP.md)
- [Build and contribute](docs/DEVELOPMENT.md)

For developers: [architecture](docs/ARCHITECTURE.md), [MCP tools](docs/MCP_CONTRACT.md), [release process](docs/RELEASING.md) and [validation results](docs/VALIDATION.md).

Created by [Umberto Giacobbi](https://github.com/umbertotechnopreneur). [MIT license](LICENSE). [Contributions](CONTRIBUTING.md) and [security reports](SECURITY.md) are welcome. Independent project; no endorsement by OpenAI, Google or Microsoft.

Brand assets and generation prompts: [brand guide](docs/BRAND.md).
