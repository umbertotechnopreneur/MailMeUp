🟩🟩🟩🟩🟩⬜⬜⬜ **62.5% source prepared · 37.5% remaining**

🟩⬜⬜⬜⬜⬜⬜⬜ **12.5% verified · 87.5% awaiting verification or later work**

**📍 Current focus:** Complete the provider registrations, then verify phases 2–5 with real accounts.
**⏭️ Next action:** Finish both provider registrations, then validate account separation, mail search and calendar agenda with known examples. See the [restart checkpoint](../.github/tasks/resume.md).

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

- ⚪ **1.1** Choose approved test accounts: two Google and two Microsoft accounts, covering personal and work accounts where available.
- 🟡 **1.2** Set up the Google and Microsoft application registrations and check consent requirements for the selected testers. The Google Cloud project `mailmeup` exists; API, consent and client completion are still to be confirmed. Microsoft remains to be created.
- ⚪ **1.3** Choose a few messages and appointments with known results to verify later.

**Ready when:** the pilot accounts and required read permissions are available. Any work-account restrictions are understood.

## 🧪 2. Connect accounts safely — source prepared, 0% verified

- 🧪 **2.1** Browser sign-in, account choice and mail/calendar scope choices compile and have local contract coverage; interactive provider checks remain.
- 🧪 **2.2** Provider credentials and account token caches use operating-system protection in source; persistence on each supported OS remains to be checked.
- 🧪 **2.3** Multiple identity slots, reconnect and local account removal are implemented in source. Restart and expired-access behavior still need real-provider checks.

**Ready when:** several accounts remain usable after restarting, and a missing or locked credential store produces a clear error.

## 🧪 3. Read email from both providers — source prepared, 0% verified

- 🧪 **3.1** Provider search is implemented in source; Gmail and Microsoft queries still need real-account comparison.
- 🧪 **3.2** Selected messages return bounded plain text. Conversation/thread reading remains outside this first slice.
- 🧪 **3.3** Bounded responses, continuation pages and account-level failures pass synthetic tests and await provider checks.

**Ready when:** results match the known examples in Gmail and Outlook, without changing unread flags or other mailbox data.

## 🧪 4. Search across accounts — source prepared, 0% verified

- 🧪 **4.1** One, several or all mail-enabled accounts can be selected in source.
- 🧪 **4.2** Global limits, 160-character previews and short local references are implemented in source.
- 🧪 **4.3** Results carry account identity and partial failures; synthetic aggregation tests pass and real multi-account behavior awaits checks.

**Ready when:** Codex can answer a cross-account request without confusing sources or presenting incomplete coverage as complete.

## 🧪 5. Add calendars and appointments — source prepared, 0% verified

- 🧪 **5.1** Calendar listing and short selection references pass synthetic tests.
- 🧪 **5.2** Combined agenda, scope-bound cursors and bounded appointment details pass synthetic tests.
- 🧪 **5.3** Provider occurrence expansion, all-day boundaries and cancellations are handled in source; time-zone and recurrence examples still need checks.

**Ready when:** the agenda matches Google and Microsoft calendars for the requested dates, with no changes or invitations sent.

## 🟡 6. Make installation usable — 33% (1/3)

- 🟢 **6.1** A short guide covers extraction, provider setup, account connection, Codex registration and sample requests.
- 🟡 **6.2** The Windows x64 archive passes smoke tests before and after extraction; a clean-machine test and macOS/Linux runs remain.
- ⚪ **6.3** Check updates preserve accounts and that diagnostics reveal no credentials or private content.

**Ready when:** a tester can install without development tools. Claim support only for platforms and account types actually tested.

## ⚪ 7. Run the real-world pilot — 0% (0/3)

- ⚪ **7.1** Start with the owner, then one or two additional testers using their own authorized accounts.
- ⚪ **7.2** Try daily email searches and agendas, including restart, lost connectivity and revoked access.
- ⚪ **7.3** Fix blocking issues and repeat the affected scenarios before sharing the next build.

**MVP accepted when:** testers can independently connect accounts, search email and check appointments; results are correct, failures are clear, credentials are protected and provider data remains unchanged.

Public rollout comes after the pilot and any remaining provider approval requirements. Sending, calendar editing, attachments and background synchronization are outside this MVP.
