🟩⬜⬜⬜⬜⬜⬜⬜ **12.5% complete · 87.5% remaining**

**📍 Current focus:** Phase 1 — Prepare the pilot; paused for a workstation restart.
**⏭️ Next action:** Restore the Edge connection and create the provider app registrations. See the [restart checkpoint](../.github/tasks/resume.md).

Progress: **1 of 8 phases complete**, counting the foundation. Phases have equal weight; this is not an estimate of development time.

🟢 Complete · 🟡 In progress · ⚪ Not started · 🧪 Awaiting requested checks

# From foundation to a real-world MVP

**Goal:** use MailMeUp in Codex to find and read email and appointments across real Google and Microsoft accounts.

**Read-only throughout:** no sending, editing, deleting, invitations or changes to messages and calendars. Local setup and credential storage can save data on the device.

**Verification rule:** the assistant finishes the assigned work, proposes the relevant tests, and runs them only when the owner explicitly requests them.

## 🟢 0. Foundation — 100% (3/3)

- 🟢 **0.1** Repository, MIT license, documentation and branding.
- 🟢 **0.2** Solution, executable, local metadata and basic MCP connection.
- 🟢 **0.3** Initial tests, CI and six-platform packaging checks completed.

**Completed:** the foundation works. Real email and calendar access is still to be built.

## 🟡 1. Prepare the pilot — 0% (0/3 complete)

- ⚪ **1.1** Choose approved test accounts: two Google and two Microsoft accounts, covering personal and work accounts where available.
- 🟡 **1.2** Set up the Google and Microsoft application registrations and check consent requirements for the selected testers. The [registration guide](APP_REGISTRATION.md) is prepared; neither registration has been created.
- ⚪ **1.3** Choose a few messages and appointments with known results to verify later.

**Ready when:** the pilot accounts and required read permissions are available. Any work-account restrictions are understood.

## ⚪ 2. Connect accounts safely — 0% (0/3)

- ⚪ **2.1** Add browser sign-in, clear account names and separate consent for email and calendars.
- ⚪ **2.2** Protect credentials using the operating system; never save plain-text tokens.
- ⚪ **2.3** Support restart, expired access, reconnect and local account removal without mixing identities.

**Ready when:** several accounts remain usable after restarting, and a missing or locked credential store produces a clear error.

## ⚪ 3. Read email from both providers — 0% (0/3)

- ⚪ **3.1** Search by words, sender and date within a chosen account.
- ⚪ **3.2** Read selected messages and conversations as clean text.
- ⚪ **3.3** Handle large messages, more result pages and unavailable accounts.

**Ready when:** results match the known examples in Gmail and Outlook, without changing unread flags or other mailbox data.

## ⚪ 4. Search across accounts — 0% (0/3)

- ⚪ **4.1** Let the user select one, several or all connected accounts.
- ⚪ **4.2** Return short previews with one overall result limit; fetch details only when requested.
- ⚪ **4.3** Identify each result's account and report any accounts that could not be searched.

**Ready when:** Codex can answer a cross-account request without confusing sources or presenting incomplete coverage as complete.

## ⚪ 5. Add calendars and appointments — 0% (0/3)

- ⚪ **5.1** List calendars and let the user select which ones to include.
- ⚪ **5.2** Show a combined agenda and open selected appointment details.
- ⚪ **5.3** Verify time zones, all-day events, recurring meetings and cancellations.

**Ready when:** the agenda matches Google and Microsoft calendars for the requested dates, with no changes or invitations sent.

## ⚪ 6. Make installation usable — 0% (0/3)

- ⚪ **6.1** Provide a short guide: download, connect accounts, register in Codex and try a sample request.
- ⚪ **6.2** Test on a clean Windows installation, then repeat on macOS and Linux, including protected sign-in.
- ⚪ **6.3** Check updates preserve accounts and that diagnostics reveal no credentials or private content.

**Ready when:** a tester can install without development tools. Claim support only for platforms and account types actually tested.

## ⚪ 7. Run the real-world pilot — 0% (0/3)

- ⚪ **7.1** Start with the owner, then one or two additional testers using their own authorized accounts.
- ⚪ **7.2** Try daily email searches and agendas, including restart, lost connectivity and revoked access.
- ⚪ **7.3** Fix blocking issues and repeat the affected scenarios before sharing the next build.

**MVP accepted when:** testers can independently connect accounts, search email and check appointments; results are correct, failures are clear, credentials are protected and provider data remains unchanged.

Public rollout comes after the pilot and any remaining provider approval requirements. Sending, calendar editing, attachments and background synchronization are outside this MVP.
