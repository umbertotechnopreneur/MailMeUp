using System.Diagnostics;
using MailMeUp.Core;
using Microsoft.Extensions.Logging;

namespace MailMeUp.Application;

/// <summary>Records bounded operation diagnostics for both adapters without logging request or result content.</summary>
public sealed class LoggingMailMeUpApplication(
    IMailMeUpApplication application,
    ILogger<LoggingMailMeUpApplication> logger) : IMailMeUpApplication
{
    /// <inheritdoc />
    public ApplicationStatus GetStatus()
    {
        logger.LogDebug("Reporting application capabilities");
        return application.GetStatus();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Account>> ListAccountsAsync(CancellationToken cancellationToken = default) =>
        RunAsync("list_accounts", () => application.ListAccountsAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Account>> ListSharedAccountsAsync(CancellationToken cancellationToken = default) =>
        RunAsync("list_shared_accounts", () => application.ListSharedAccountsAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AccountSharingSettings>> ListAccountSharingAsync(CancellationToken cancellationToken = default) =>
        RunAsync("list_account_sharing", () => application.ListAccountSharingAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<AccountSharingSettings> SaveAccountSharingAsync(AccountSharingSettings settings, CancellationToken cancellationToken = default) =>
        RunAsync("save_account_sharing", () => application.SaveAccountSharingAsync(settings, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderCalendar>> ListAvailableCalendarsAsync(string accountId, CancellationToken cancellationToken = default) =>
        RunAsync("discover_calendars_for_setup", () => application.ListAvailableCalendarsAsync(accountId, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderSetupStatus>> ListProviderSetupAsync(CancellationToken cancellationToken = default) =>
        RunAsync("setup_status", () => application.ListProviderSetupAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<ProviderSetupResult> ConfigureProviderAsync(string providerId, string source, CancellationToken cancellationToken = default) =>
        RunAsync("configure_provider", () => application.ConfigureProviderAsync(providerId, source, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<AccountConnectionResult> ConnectAccountAsync(string providerId, AccountConnectionOptions options, CancellationToken cancellationToken = default) =>
        RunAsync("connect_account", () => application.ConnectAccountAsync(providerId, options, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<AccountRemovalResult> RemoveAccountAsync(string accountId, CancellationToken cancellationToken = default) =>
        RunAsync("remove_account", () => application.RemoveAccountAsync(accountId, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<MailSearchResult> SearchMailAsync(MailSearchRequest request, CancellationToken cancellationToken = default) =>
        RunAsync("search_mail", () => application.SearchMailAsync(request, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<MailMessageResult> ReadMailAsync(MailReadRequest request, CancellationToken cancellationToken = default) =>
        RunAsync("read_mail", () => application.ReadMailAsync(request, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<CalendarListResult> ListCalendarsAsync(CalendarListRequest request, CancellationToken cancellationToken = default) =>
        RunAsync("list_calendars", () => application.ListCalendarsAsync(request, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<EventSearchResult> SearchEventsAsync(EventSearchRequest request, CancellationToken cancellationToken = default) =>
        RunAsync("search_events", () => application.SearchEventsAsync(request, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<EventResult> ReadEventAsync(EventReadRequest request, CancellationToken cancellationToken = default) =>
        RunAsync("read_event", () => application.ReadEventAsync(request, cancellationToken), cancellationToken);

    private async Task<T> RunAsync<T>(string operation, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        logger.LogDebug("Operation {Operation} started", operation);
        try
        {
            var result = await action();
            logger.LogInformation("Operation {Operation} completed in {ElapsedMs} ms", operation, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            var failedAccounts = result switch
            {
                MailSearchResult mail => mail.FailedAccounts.Count,
                CalendarListResult calendars => calendars.FailedAccounts.Count,
                EventSearchResult events => events.FailedAccounts.Count,
                _ => 0
            };
            if (failedAccounts > 0)
            {
                logger.LogWarning("Operation {Operation} returned partial coverage; {FailedAccountCount} accounts unavailable", operation, failedAccounts);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Operation {Operation} cancelled after {ElapsedMs} ms", operation, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            // Never pass the exception itself: its message or inner exceptions can contain private data.
            logger.LogWarning("Operation {Operation} failed after {ElapsedMs} ms ({ErrorType})", operation,
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds, exception.GetType().Name);
            throw;
        }
    }
}
