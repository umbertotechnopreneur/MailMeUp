🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩⬜⬜⬜⬜⬜⬜⬜ **12.5% verified · 87.5% awaiting verification or later work**

**📍 Current focus:** The two-account Google read-only checkpoint passes. Complete the Microsoft registration and verify provider-specific edge cases.
**⏭️ Next action:** Check a known upcoming Google appointment, then register Microsoft and repeat the same mail/calendar flow. See the [restart checkpoint](../.github/tasks/resume.md).

Source readiness: **5 of 8 phases have their planned code**, including the foundation. Verified progress: **1 of 8 phases**. Phases have equal weight; these percentages are status markers, not estimates of development time.

🟢 Complete · 🟡 In progress · ⚪ Not started · 🧪 Automated checks passed; real-provider checks pending

# From foundation to a real-world MVP

**Goal:** use MailMeUp in Codex to find and read email and appointments across real Google and Microsoft accounts.

**Read-only throughout:** no sending, editing, deleting, invitations or changes to messages and calendars. Local setup and credential storage can save data on the device.

**Verification rule:** the assistant finishes the assigned work, proposes the relevant tests, and runs them only when the owner explicitly requests them.

## 🟢 0. Foundation — 100% (3/3)

- 🟢 **0.1** Repository, MIT license, documentation and branding.
- 🟢 **0.2** Solution, executable, local metadata and basic MCP connection.
- 🟢 **0.3** Initial tests, CI and six-platform packaging checks completed.

**Completed:** the foundation works. Real email and calendar code now passes local automated checks but remains unverified against providers.

## 🟡 1. Prepare the pilot — 0% (0/3 complete)

- 🟡 **1.1** Two approved Google test accounts are connected. Microsoft test accounts still need to be selected.
- 🟡 **1.2** The Google Desktop client is configured locally, remains in Testing and accepts approved test users. A non-approved account was correctly denied. Publication was not attempted. Microsoft remains to be created.
- ⚪ **1.3** Choose a few messages and appointments with known results to verify later.

**Ready when:** the pilot accounts and required read permissions are available. Any work-account restrictions are understood.

## 🧪 2. Connect accounts safely — source prepared, 0% verified

- 🧪 **2.1** Browser sign-in and the six allowed read-only scopes pass for two Google accounts. Microsoft remains.
- 🧪 **2.2** Google client and account tokens persist through new Windows processes using protected storage. macOS and Linux are outside current validation.
- 🧪 **2.3** Two Google identities remain separate and usable after restart. Real reconnect, expired-access and removal checks remain.

**Ready when:** several accounts remain usable after restarting, and a missing or locked credential store produces a clear error.

## 🧪 3. Read email from both providers — source prepared, 0% verified

- 🧪 **3.1** Real Gmail search covers both connected accounts. Microsoft comparison remains.
- 🧪 **3.2** A real Gmail result returns bounded plain text. Conversation/thread reading remains outside this first slice.
- 🧪 **3.3** Real Gmail compact results and continuation pass. Real provider-failure handling remains.

**Ready when:** results match the known examples in Gmail and Outlook, without changing unread flags or other mailbox data.

## 🧪 4. Search across accounts — source prepared, 0% verified

- 🧪 **4.1** A real all-account search covers two Google accounts. Microsoft and mixed-provider selection remain.
- 🧪 **4.2** The real search respects the global limit and supports short references and a continuation cursor.
- 🧪 **4.3** Results preserve their source account with complete two-account coverage. A real partial-failure case remains.

**Ready when:** Codex can answer a cross-account request without confusing sources or presenting incomplete coverage as complete.

## 🧪 5. Add calendars and appointments — source prepared, 0% verified

- 🧪 **5.1** Four real Google calendars were listed across two accounts with complete coverage.
- 🧪 **5.2** Real all-calendar search, appointment detail and continuation pass. The primary-calendar window can return an empty agenda correctly.
- 🧪 **5.3** Provider occurrence expansion, all-day boundaries and cancellations are handled in source; known time-zone, recurrence and cancellation examples still need comparison.

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

Public rollout comes after the pilot and any remaining provider approval requirements. Sending, calendar editing, attachments and background synchronization are outside this MVP.
