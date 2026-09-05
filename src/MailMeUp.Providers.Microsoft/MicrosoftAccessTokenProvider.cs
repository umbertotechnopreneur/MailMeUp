using MailMeUp.Core;
using MailMeUp.Security;
using Microsoft.Identity.Client;

namespace MailMeUp.Providers.Microsoft;

internal sealed class MicrosoftAccessTokenProvider(IProviderConfigurationStore configurations, ISecretStore secrets)
{
    private const string AccountIdPrefix = "microsoft:";

    public async Task<string> GetAsync(
        Account account,
        IReadOnlyList<string> scopes,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(account.Provider, "microsoft", StringComparison.Ordinal) ||
            !account.Id.StartsWith(AccountIdPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("The account does not belong to Microsoft.", nameof(account));
        }

        var configuration = await configurations.GetAsync("microsoft", cancellationToken)
            ?? throw new ProviderReadException("Microsoft app setup is missing.");
        try
        {
            return await MicrosoftIdentitySession.RunAsync(
                configuration.ClientId,
                secrets,
                async application =>
                {
                    var homeAccountId = account.Id[AccountIdPrefix.Length..];
                    var msalAccount = (await application.GetAccountsAsync())
                        .SingleOrDefault(item => string.Equals(item.HomeAccountId?.Identifier, homeAccountId, StringComparison.Ordinal))
                        ?? throw new ProviderReadException("The Microsoft account credential is missing. Reconnect the account.");
                    try
                    {
                        var result = await application.AcquireTokenSilent(scopes, msalAccount).ExecuteAsync(cancellationToken);
                        return string.IsNullOrWhiteSpace(result.AccessToken)
                            ? throw new ProviderReadException("Microsoft access expired. Reconnect the account.")
                            : result.AccessToken;
                    }
                    catch (MsalUiRequiredException)
                    {
                        throw new ProviderReadException("Microsoft access expired. Reconnect the account.");
                    }
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ProviderReadException)
        {
            throw;
        }
        catch (SecretStoreException)
        {
            throw new ProviderReadException("The protected Microsoft credential could not be accessed. Check local credential storage.");
        }
        catch (Exception)
        {
            throw new ProviderReadException("Microsoft authorization is temporarily unavailable. Try again later.");
        }
    }
}
