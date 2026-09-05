# Register MailMeUp with Google and Microsoft

An app registration gives MailMeUp an identity at each provider. It produces a **Client ID**. Each person still signs in and chooses whether to let the app read their data.

## Google

1. Open [Google Cloud Console](https://console.cloud.google.com/) and create a project named **MailMeUp**.
2. In **APIs & Services > Library**, enable **Gmail API** and **Google Calendar API**.
3. Open **Google Auth Platform**. Set the app name and contact email, choose **External** for personal and work accounts, and add your pilot accounts as test users. Keep the app in testing for the initial pilot.
4. Under **Data Access**, select only the email/calendar read scopes required by MailMeUp, plus basic account identity.
5. Under **Clients > Create client**, choose **Desktop app** and name it **MailMeUp Desktop**. Keep the Client ID and downloaded client configuration privately on your computer.

Calendar permission changes require a new consent flow for this desktop app. Do not assume Google supports automatic incremental consent for installed apps.

## Microsoft

1. Open [Microsoft Entra](https://entra.microsoft.com/) using a directory where you can register applications.
2. Go to **Entra ID > App registrations > New registration**. Name the app **MailMeUp**.
3. To support Outlook.com and Microsoft 365, select the account type covering **any Entra ID tenant and personal Microsoft accounts**.
4. Add the **Mobile and desktop applications** platform with **http://localhost** as its browser callback.
5. Add Microsoft Graph **delegated** read permissions for mail and calendars, plus basic profile access. Record the **Application (client) ID**. This desktop application does not need a client secret.

Work directories may require administrator approval. For the first pilot, use accounts you are authorized to connect.

## What comes next

Configure these app identities locally, then sign in to each mailbox. Tokens are created during sign-in; you do not create or paste account tokens manually. Never commit client configuration or credentials to GitHub.

Sources: [Google client creation](https://developers.google.com/workspace/guides/create-credentials), [Google desktop OAuth](https://developers.google.com/identity/protocols/oauth2/native-app), [Microsoft registration](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app), [Microsoft desktop callback](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-app-configuration).
