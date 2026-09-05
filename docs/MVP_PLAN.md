🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% verified · 37.5% awaiting verification or later work**

**📍 Current focus:** Real examples pass across four accounts. Verify account recovery and installation behavior.
**⏭️ Next action:** Check reconnect, revoked access, local removal and partial-provider failures. See the [restart checkpoint](../.github/tasks/resume.md).

Source readiness: **5 of 8 phases have their planned code**, including the foundation. Verified progress: **15 of 24 checkpoints**. Each phase has three checkpoints; percentages are status markers, not estimates of development time.

🟢 Complete · 🟡 In progress · ⚪ Not started · 🧪 Automated checks passed; real-provider checks pending

# From foundation to a real-world MVP

**Goal:** use MailMeUp in Codex to find and read email and appointments across real Google and Microsoft accounts.

**Read-only throughout:** no sending, editing, deleting, invitations or changes to messages and calendars. MailMeUp cannot create, update or remove calendar events. Local setup and credential storage can save data on the device.

**Verification rule:** the assistant finishes the assigned work, proposes the relevant tests, and runs them only when the owner explicitly requests them.

## 🟢 0. Foundation — 100% (3/3)

- 🟢 **0.1** Repository, MIT license, documentation and branding.
- 🟢 **0.2** Solution, executable, local metadata and basic MCP connection.
- 🟢 **0.3** Initial tests, CI and six-platform packaging checks completed.

**Completed:** the foundation works. Current email and calendar reads pass local checks and privacy-preserving real-provider checks.

## 🟢 1. Prepare the pilot — 100% (3/3)

- 🟢 **1.1** Two Google and two Microsoft test accounts are connected with separate identities.
- 🟢 **1.2** Both desktop registrations are configured locally. Google remains in Testing; no provider app was published.
- 🟢 **1.3** Bounded real examples were read and checked without writing identities or content to test output.

**Ready when:** the pilot accounts and required read permissions are available. Any work-account restrictions are understood.

## 🟡 2. Connect accounts safely — 67% (2/3)

- 🟢 **2.1** Browser sign-in and read-only mail/calendar consent pass for two Google and two Microsoft accounts.
- 🟢 **2.2** All four account tokens persist through new Windows processes using protected storage. macOS and Linux are outside current validation.
- 🟡 **2.3** Multiple identities remain separate and usable after restart. Real reconnect, expired-access and removal checks remain.

**Ready when:** several accounts remain usable after restarting, and a missing or locked credential store produces a clear error.

## 🟡 3. Read email from both providers — 67% (2/3)

- 🟢 **3.1** Real Gmail and Microsoft searches cover two accounts per provider.
- 🟢 **3.2** Real messages from both providers return bounded plain text. Conversation/thread reading remains outside this first slice.
- 🟡 **3.3** Compact results, bounded detail consistency and continuation pass for all four accounts. Real provider-failure handling remains.

**Ready when:** results match the known examples in Gmail and Outlook, without changing unread flags or other mailbox data.

## 🟡 4. Search across accounts — 67% (2/3)

- 🟢 **4.1** A real mixed search completes across all four Google and Microsoft accounts.
- 🟢 **4.2** Real searches respect global limits and support short references and continuation cursors.
- 🟡 **4.3** Results preserve their source account with complete four-account coverage. A real partial-failure case remains.

**Ready when:** Codex can answer a cross-account request without confusing sources or presenting incomplete coverage as complete.

## 🟡 5. Read calendars and appointments — 67% (2/3)

- 🟢 **5.1** Seven real calendars were listed across all four accounts with complete coverage.
- 🟢 **5.2** Mixed-provider agenda, bounded appointment detail, continuation and empty windows pass. Microsoft null optional event fields are handled.
- 🟡 **5.3** Real event summary/detail examples agree for three accounts; the fourth has no events in the checked window. Known time-zone, recurrence and cancellation examples still need comparison.

**Ready when:** the agenda matches Google and Microsoft calendars for the requested dates, with no changes or invitations sent.

## 🟡 6. Make installation usable — 33% (1/3)

- 🟢 **6.1** A short guide covers extraction, provider setup, account connection, Codex registration and sample requests.
- 🟡 **6.2** The Windows x64 archive passes smoke tests before and after extraction; a clean Windows machine remains. Windows ARM64 is build-only. macOS and Linux will not be tested without suitable machines.
- ⚪ **6.3** Check updates preserve accounts and that diagnostics reveal no credentials or private content.

**Ready when:** a Windows x64 tester can install without development tools. Support remains limited to platforms and account types actually tested.

## ⚪ 7. Run the real-world pilot — 0% (0/3)

- ⚪ **7.1** Start with the owner, then one or two additional testers using their own authorized accounts.
- ⚪ **7.2** Try daily email searches and agendas, including restart, lost connectivity and revoked access.
- ⚪ **7.3** Fix blocking issues and repeat the affected scenarios before sharing the next build.

**MVP accepted when:** testers can independently connect accounts, search email and check appointments; results are correct, failures are clear, credentials are protected and provider data remains unchanged.

Public rollout comes after the pilot and any remaining provider approval requirements. Sending, creating or editing calendar events, attachments and background synchronization are outside this MVP.
