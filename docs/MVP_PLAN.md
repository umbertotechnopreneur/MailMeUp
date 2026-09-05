🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩🟩🟩🟩🟩🟨⬜⬜ **66.7% verified · 33.3% awaiting verification or later work**

**📍 Current focus:** Clean Windows installation and preparation for the owner pilot.
**⏭️ Next action:** Install on a clean Windows x64 machine, then compare known mail and calendar examples. See the [restart checkpoint](../.github/tasks/resume.md).

Source readiness: **5 of 8 phases have their planned code**, including the foundation. Verified progress: **16 of 24 checkpoints**. Each phase has three checkpoints; percentages are status markers, not estimates of development time.

**Latest checks:** 68 .NET tests, 24 manual-runner regressions and 27 real-provider checks passed on Windows; three event checks were skipped. Synthetic fault tests do not replace real recovery or independent result comparison.

**Canonical plan:** this versioned repository file is the source of truth; external output copies are retired.

🟢 Complete · 🟡 In progress · ⚪ Not started · 🧪 Automated checks passed; real-provider checks pending

# From foundation to a real-world MVP

**Goal:** use MailMeUp in Codex to find and read email and appointments across real Google and Microsoft accounts.

**Read-only throughout:** no sending, editing, deleting, invitations or changes to messages and calendars. MailMeUp cannot create, update or remove calendar events. Local setup and credential storage can save data on the device.

**Verification rule:** the assistant finishes the assigned work, proposes the relevant tests, and runs them only when the owner explicitly requests them.

## 🟢 0. Foundation — 100% (3/3)

- 🟢 **0.1** Repository, MIT license, documentation and branding.
- 🟢 **0.2** Solution, executable, local metadata and basic MCP connection.
- 🟢 **0.3** Initial tests, CI and six-platform packaging checks completed.

**Completed:** the foundation works. The earlier Windows build passed local checks and privacy-preserving real-provider reads.

## 🟢 1. Prepare the pilot — 100% (3/3)

- 🟢 **1.1** Two Google and two Microsoft test accounts are connected with separate identities.
- 🟢 **1.2** Both desktop registrations are configured locally. Google remains in Testing; no provider app was published.
- 🟢 **1.3** Bounded real examples passed summary/detail consistency checks without writing identities or content to test output. Independent comparison with the provider UI remains.

**Ready when:** the pilot accounts and required read permissions are available. Any work-account restrictions are understood.

## 🟡 2. Connect accounts safely — 67% (2/3)

- 🟢 **2.1** Browser sign-in and read-only mail/calendar consent pass for two Google and two Microsoft accounts.
- 🟢 **2.2** All four account tokens persist through new Windows processes using protected storage. macOS and Linux are outside current validation.
- 🧪 **2.3** Failed reconnect preservation, credential sessions and selective local removal passed synthetic tests. Real expiry, revoked access and reconnect remain.

**Ready when:** several accounts remain usable after restarting, and a missing or locked credential store produces a clear error.

## 🟡 3. Read email from both providers — 67% (2/3)

- 🟢 **3.1** Real Gmail and Microsoft searches cover two accounts per provider.
- 🟢 **3.2** Real messages from both providers return bounded plain text. Conversation/thread reading remains outside this first slice.
- 🧪 **3.3** Compact results and bounded details passed for four real accounts; continuation failure handling passed synthetic tests. Independent comparison with known provider results remains.

**Ready when:** results match the known examples in Gmail and Outlook, without changing unread flags or other mailbox data.

## 🟡 4. Search across accounts — 67% (2/3)

- 🟢 **4.1** A real mixed search completes across all four Google and Microsoft accounts.
- 🟢 **4.2** Real searches respect global limits and support short references and continuation cursors.
- 🧪 **4.3** Partial failures, removed accounts, short/empty pages and continuation limits passed synthetic checks. The runner reports accounts separately and refuses CI. A deliberate real connectivity failure remains.

**Ready when:** Codex can answer a cross-account request without confusing sources or presenting incomplete coverage as complete.

## 🟡 5. Read calendars and appointments — 67% (2/3)

- 🟢 **5.1** Seven real calendars were listed across all four accounts with complete coverage.
- 🟢 **5.2** Mixed-provider agenda, bounded appointment detail, continuation and empty windows pass. Microsoft null optional event fields are handled.
- 🧪 **5.3** Summary/detail checks passed for three accounts, with an empty fourth. Date/time-zone, null-field and pagination regressions passed synthetic tests; real recurrence and cancellation examples still need comparison.

**Ready when:** the agenda matches Google and Microsoft calendars for the requested dates, with no changes or invitations sent.

## 🟡 6. Make installation usable — 67% (2/3)

- 🟢 **6.1** A short guide covers extraction, provider setup, account connection, Codex registration and sample requests.
- 🟡 **6.2** The Windows x64 archive from `de1fae7` passes smoke tests before and after extraction and live reads; a clean Windows machine remains. Windows ARM64 is build-only. macOS and Linux will not be tested without suitable machines.
- 🟢 **6.3** Updating from `971a5b8` to `de1fae7` preserved all four accounts and their metadata on the Windows workstation. Synthetic diagnostics checks reveal no credentials or private content.

**Ready when:** a Windows x64 tester can install without development tools. Support remains limited to platforms and account types actually tested.

## ⚪ 7. Run the real-world pilot — 0% (0/3)

- ⚪ **7.1** Start with the owner, then one or two additional testers using their own authorized accounts.
- ⚪ **7.2** Try daily email searches and agendas, including restart, lost connectivity and revoked access.
- ⚪ **7.3** Fix blocking issues and repeat the affected scenarios before sharing the next build.

**MVP accepted when:** testers can independently connect accounts, search email and check appointments; results are correct, failures are clear, credentials are protected and provider data remains unchanged.

Public rollout comes after the pilot and any remaining provider approval requirements. Sending, creating or editing calendar events, attachments and background synchronization are outside this MVP.
