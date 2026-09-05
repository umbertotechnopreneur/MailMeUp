using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using MailMeUp.Core;
using MailMeUp.Security;

namespace MailMeUp.Providers.Google;

internal sealed class GoogleAccessTokenProvider(IProviderConfigurationStore configurations, ISecretStore secrets)
{
    private const string MailScope = "https://www.googleapis.com/auth/gmail.readonly";
    private const string CalendarListScope = "https://www.googleapis.com/auth/calendar.calendarlist.readonly";
    private const string CalendarEventsScope = "https://www.googleapis.com/auth/calendar.events.readonly";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);

    public async Task<string> GetAsync(Account account, CancellationToken cancellationToken)
    {
        if (!string.Equals(account.Provider, "google", StringComparison.Ordinal))
        {
            throw new ArgumentException("The account does not belong to Google.", nameof(account));
        }

        var configuration = await configurations.GetAsync("google", cancellationToken)
            ?? throw new ProviderReadException("Google app setup is missing.");
        if (string.IsNullOrWhiteSpace(configuration.ClientSecretReference))
        {
            throw new ProviderReadException("The protected Google client credential is missing.");
        }

        var gate = Gates.GetOrAdd(account.Id, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        byte[]? secretBytes = null;
        try
        {
            secretBytes = await secrets.ReadAsync(configuration.ClientSecretReference, cancellationToken)
                ?? throw new ProviderReadException("The protected Google client credential is missing.");
            var tokenStore = new ProtectedGoogleTokenStore(secrets, account.Id);
            using var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = configuration.ClientId,
                    ClientSecret = Encoding.UTF8.GetString(secretBytes)
                },
                Scopes = CreateScopes(account),
                DataStore = tokenStore
            });
            var token = await flow.LoadTokenAsync(account.Id, cancellationToken)
                ?? throw new ProviderReadException("The Google account credential is missing. Reconnect the account.");
            var credential = new UserCredential(flow, account.Id, token);
            var accessToken = await credential.GetAccessTokenForRequestAsync(null, cancellationToken);
            return string.IsNullOrWhiteSpace(accessToken)
                ? throw new ProviderReadException("Google access expired. Reconnect the account.")
                : accessToken;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ProviderReadException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ProviderReadException("Google access expired or is unavailable. Reconnect the account.");
        }
        finally
        {
            if (secretBytes is not null)
            {
                CryptographicOperations.ZeroMemory(secretBytes);
            }

            gate.Release();
        }
    }

    private static IReadOnlyList<string> CreateScopes(Account account)
    {
        var scopes = new List<string> { "openid", "email", "profile" };
        if (account.MailReadEnabled)
        {
            scopes.Add(MailScope);
        }

        if (account.CalendarReadEnabled)
        {
            scopes.Add(CalendarListScope);
            scopes.Add(CalendarEventsScope);
        }

        return scopes;
    }
}
