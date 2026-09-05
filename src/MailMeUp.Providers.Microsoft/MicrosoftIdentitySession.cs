using System.Security.Cryptography;
using MailMeUp.Security;
using Microsoft.Identity.Client;

namespace MailMeUp.Providers.Microsoft;

internal static class MicrosoftIdentitySession
{
    private const string TokenCacheReference = "providers/microsoft/token-cache";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<T> RunAsync<T>(
        string clientId,
        ISecretStore secrets,
        Func<IPublicClientApplication, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var application = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                .WithRedirectUri("http://localhost")
                .Build();
            AttachProtectedCache(application.UserTokenCache, secrets);
            return await operation(application);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static void AttachProtectedCache(ITokenCache tokenCache, ISecretStore secrets)
    {
        tokenCache.SetBeforeAccessAsync(async notification =>
        {
            var bytes = await secrets.ReadAsync(TokenCacheReference);
            if (bytes is null)
            {
                return;
            }

            try
            {
                notification.TokenCache.DeserializeMsalV3(bytes, shouldClearExistingCache: true);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        });
        tokenCache.SetAfterAccessAsync(async notification =>
        {
            if (!notification.HasStateChanged)
            {
                return;
            }

            var bytes = notification.TokenCache.SerializeMsalV3();
            try
            {
                if (bytes.Length == 0)
                {
                    await secrets.DeleteAsync(TokenCacheReference);
                }
                else
                {
                    await secrets.WriteAsync(TokenCacheReference, bytes);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        });
    }
}
