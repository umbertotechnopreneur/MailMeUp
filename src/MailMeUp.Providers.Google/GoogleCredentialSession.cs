using MailMeUp.Security;

namespace MailMeUp.Providers.Google;

internal static class GoogleCredentialSession
{
    public static ValueTask<IDisposable> AcquireAsync(
        ISecretStore secrets,
        string accountId,
        CancellationToken cancellationToken) =>
        secrets.AcquireSessionAsync(ProtectedGoogleTokenStore.CreateReference(accountId), cancellationToken);
}
