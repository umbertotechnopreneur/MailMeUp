# Provider setup and first sign-in

> [!IMPORTANT]
> **Temporary early-build requirement.** MailMeUp does not yet provide a shared provider-registration or one-click onboarding flow. Until that changes, each user must create their own Google and/or Microsoft desktop app registration before connecting an account. We are actively working to make this simpler.

> [!WARNING]
> **Platform limit as of 2026-09-05.** Google and Microsoft browser sign-in has been exercised on Windows x64 only. The OAuth flow and protected token storage on macOS and Linux are not yet verified and must be tested before support can be claimed.

## Why this is needed

Google and Microsoft do not allow a desktop program to read a mailbox just because the program is installed. OAuth is the consent process that opens the provider's sign-in page, shows the requested read-only permissions, and lets the account owner approve or decline them.

A **Client ID** identifies the local MailMeUp desktop app to the provider. It is not an account password. Google gives you a downloadable desktop-client JSON file; keep that file private because it includes the app configuration. Microsoft gives you an **Application (client) ID**; a Microsoft desktop app does not need a client secret.

The two provider flows are independent. Configure and connect only the providers you plan to use. Each account signs in separately, and MailMeUp stores its tokens only in protected operating-system storage.

## Before you begin

Use the full path to your local executable. In these examples, replace the path with the folder where you installed or built MailMeUp:

```powershell
$MailMeUp = 'C:\Tools\MailMeUp\mailmeup.exe'
& $MailMeUp --help
```

The commands below use `$MailMeUp` so that you do not need to change into the executable's folder. Never paste JSON contents, tokens, refresh tokens, or client secrets into a terminal transcript, a chat, or GitHub.

## Google: register the app and connect an account

1. Open [Google Cloud Console](https://console.cloud.google.com/) and select or create a project for MailMeUp.
2. In **APIs & Services > Library**, enable only **Gmail API** and **Google Calendar API**.
3. Open **Google Auth Platform**:
   - set the app name and contact email;
   - choose **External** if personal or other Google accounts will sign in;
   - keep the app in **Testing** for the pilot;
   - add the exact Google accounts that may test the app.
4. Under **Data Access**, add only these scopes:

   ```text
   openid
   email
   profile
   https://www.googleapis.com/auth/gmail.readonly
   https://www.googleapis.com/auth/calendar.calendarlist.readonly
   https://www.googleapis.com/auth/calendar.events.readonly
   ```

5. Under **Clients**, create a **Desktop app** client named **MailMeUp Desktop**. Download its JSON file and keep it in a private local folder.
6. Import that file into MailMeUp, then open the browser consent flow:

   ```powershell
   & $MailMeUp setup google 'C:\Private\client_secret.json'
   & $MailMeUp setup status
   & $MailMeUp accounts connect google
   ```

7. Sign in with one of the configured Google test users and approve the read-only permissions. Run the last command again for every additional Google account.

Do not publish the Google OAuth app, create API keys, use service accounts, or request Gmail write/send scopes or calendar write scopes for this pilot.

## Microsoft: register the app and connect an account

1. Open [Microsoft Entra](https://entra.microsoft.com/) with a directory where you can register applications. Go to **Entra ID > App registrations > New registration**.
2. Name the app **MailMeUp Desktop**.
3. To support both Outlook.com and Microsoft 365, choose **Accounts in any organizational directory and personal Microsoft accounts**. Choose a narrower audience only when you deliberately want to limit who can sign in.
4. In **Authentication**, add the **Mobile and desktop applications** platform with this redirect URI:

   ```text
   http://localhost
   ```

5. In **API permissions > Microsoft Graph > Delegated permissions**, retain `User.Read` and add only:

   ```text
   Mail.Read
   Calendars.Read
   ```

6. Do not create a certificate or client secret. Do not add application permissions, `Mail.ReadWrite`, `Mail.Send`, or `Calendars.ReadWrite`. Do not grant tenant-wide admin consent unless your organization explicitly requires and approves it.
7. On **Overview**, copy the **Application (client) ID**. Configure MailMeUp and start browser sign-in:

   ```powershell
   & $MailMeUp setup microsoft '<application-client-id>'
   & $MailMeUp setup status
   & $MailMeUp accounts connect microsoft
   ```

8. Choose the Microsoft account in the browser and approve the delegated read-only permissions. Run the last command again for each additional Microsoft account.

Some work or school directories apply their own consent policy. Do not try to bypass that policy; ask the directory administrator if the provider blocks consent.

## Check connected accounts

```powershell
& $MailMeUp accounts list
```

Use `--mail-only` or `--calendar-only` with `accounts connect` when you want to grant just one read category. Use `accounts remove <account-id>` to remove local account metadata and the protected cached credentials; revoke the provider grant separately in the Google or Microsoft account settings.

MailMeUp remains read-only for this milestone: it does not send email, edit or delete messages, create or edit calendar events, or send invitations.
