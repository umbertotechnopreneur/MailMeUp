using System.Collections.Concurrent;
using System.Security.Cryptography;
using MailMeUp.Core;

namespace MailMeUp.Application;

/// <summary>Coordinates account lifecycle and compact read-only operations across providers.</summary>
public sealed class MailMeUpApplication : IMailMeUpApplication
{
    private readonly IAccountStore _accounts;
    private readonly IReadOnlyList<ProviderDescriptor> _providers;
    private readonly IReadOnlyDictionary<string, IProviderSetupService> _providerSetupServices;
    private readonly IReadOnlyDictionary<string, IAccountConnector> _accountConnectors;
    private readonly IReadOnlyDictionary<string, IMailReader> _mailReaders;
    private readonly IReadOnlyDictionary<string, ICalendarReader> _calendarReaders;
    private readonly LocalReferenceStore _references = new();

    /// <summary>Creates the complete application boundary used by the local CLI and MCP host.</summary>
    public MailMeUpApplication(
        IAccountStore accounts,
        IEnumerable<IProviderModule> providers,
        IEnumerable<IProviderSetupService> providerSetupServices,
        IEnumerable<IAccountConnector> accountConnectors,
        IEnumerable<IMailReader> mailReaders,
        IEnumerable<ICalendarReader> calendarReaders)
    {
        _accounts = accounts;
        _providerSetupServices = providerSetupServices.ToDictionary(service => service.ProviderId, StringComparer.Ordinal);
        _accountConnectors = accountConnectors.ToDictionary(connector => connector.ProviderId, StringComparer.Ordinal);
        _mailReaders = mailReaders.ToDictionary(reader => reader.ProviderId, StringComparer.Ordinal);
        _calendarReaders = calendarReaders.ToDictionary(reader => reader.ProviderId, StringComparer.Ordinal);
        _providers = providers
            .Select(provider => provider.Descriptor with
            {
                AuthenticationAvailable = _accountConnectors.ContainsKey(provider.Descriptor.Id),
                MailReadAvailable = _mailReaders.ContainsKey(provider.Descriptor.Id),
                CalendarReadAvailable = _calendarReaders.ContainsKey(provider.Descriptor.Id)
            })
            .OrderBy(provider => provider.Id, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Creates the foundation application without setup, account connection or provider-read services.</summary>
    public MailMeUpApplication(IAccountStore accounts, IEnumerable<IProviderModule> providers)
        : this(accounts, providers, [], [], [], [])
    {
    }

    /// <inheritdoc />
    public ApplicationStatus GetStatus() => new(
        _calendarReaders.Count > 0 && _mailReaders.Count > 0
            ? "read_only_mvp"
            : _mailReaders.Count > 0
                ? "mail_read"
                : _accountConnectors.Count > 0 ? "account_setup" : "foundation",
        "stdio",
        true,
        _accountConnectors.Count > 0,
        _providers);

    /// <inheritdoc />
    public Task<IReadOnlyList<Account>> ListAccountsAsync(CancellationToken cancellationToken = default) =>
        _accounts.ListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProviderSetupStatus>> ListProviderSetupAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<ProviderSetupStatus>(_providerSetupServices.Count);
        foreach (var service in _providerSetupServices.Values.OrderBy(item => item.ProviderId, StringComparer.Ordinal))
        {
            statuses.Add(await service.GetStatusAsync(cancellationToken));
        }

        return statuses;
    }

    /// <inheritdoc />
    public Task<ProviderSetupResult> ConfigureProviderAsync(string providerId, string source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (!_providerSetupServices.TryGetValue(providerId, out var service))
        {
            throw new ArgumentException("Unknown provider. Run mailmeup setup status to list supported providers.", nameof(providerId));
        }

        return service.ConfigureAsync(source, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AccountConnectionResult> ConnectAccountAsync(
        string providerId,
        AccountConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (!_accountConnectors.TryGetValue(providerId, out var connector))
        {
            throw new ArgumentException("Unknown account provider. Run mailmeup setup status to list supported providers.", nameof(providerId));
        }

        var result = await connector.ConnectAsync(options, cancellationToken);
        try
        {
            await _accounts.SaveAsync(result.Account, cancellationToken);
            return result;
        }
        catch
        {
            try
            {
                await connector.DisconnectAsync(result.Account, CancellationToken.None);
            }
            catch (ProviderAuthenticationException)
            {
                // Preserve the metadata failure; later setup can overwrite the unreachable protected credential.
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AccountRemovalResult> RemoveAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        var account = (await _accounts.ListAsync(cancellationToken))
            .SingleOrDefault(item => string.Equals(item.Id, accountId, StringComparison.Ordinal));
        if (account is null)
        {
            return new AccountRemovalResult(accountId, Removed: false);
        }

        if (!_accountConnectors.TryGetValue(account.Provider, out var connector))
        {
            throw new InvalidOperationException("The account provider is not available in this build.");
        }

        await connector.DisconnectAsync(account, cancellationToken);
        var removed = await _accounts.DeleteAsync(account.Id, cancellationToken);
        return new AccountRemovalResult(account.Id, removed);
    }

    /// <inheritdoc />
    public async Task<MailSearchResult> SearchMailAsync(MailSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var query = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length > 500)
        {
            throw new ArgumentException("Mail search text must contain 1 to 500 characters.", nameof(request));
        }

        if (request.Limit is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Mail search limit must be between 1 and 50.");
        }

        var sender = string.IsNullOrWhiteSpace(request.Sender) ? null : request.Sender.Trim();
        if (sender is not null && (sender.Length > 254 || sender.Any(char.IsControl)))
        {
            throw new ArgumentException("The optional mail sender is invalid.", nameof(request));
        }

        var start = ParseOptionalExplicitDateTime(request.Start, nameof(request.Start));
        var end = ParseOptionalExplicitDateTime(request.End, nameof(request.End));
        if (start is not null && end is not null && end.Value <= start.Value)
        {
            throw new ArgumentException("The mail end time must be later than the start time.", nameof(request));
        }

        var providerQuery = new ProviderMailQuery(query, sender, start, end);

        var allAccounts = await _accounts.ListAsync(cancellationToken);
        MailCursorState state;
        IReadOnlyList<Account> selectedAccounts;
        if (string.IsNullOrWhiteSpace(request.Cursor))
        {
            selectedAccounts = SelectMailAccounts(allAccounts, request.AccountIds);
            state = new MailCursorState(
                providerQuery,
                selectedAccounts.Select(account => account.Id).ToArray(),
                selectedAccounts.ToDictionary(
                    account => account.Id,
                    _ => new AccountMailCursorState(),
                    StringComparer.Ordinal));
        }
        else
        {
            if (request.Cursor.Length > 128)
            {
                throw new ArgumentException("The mail cursor is invalid.", nameof(request));
            }

            state = _references.Get<MailCursorState>(request.Cursor, "c_").Copy();
            if (state.Query != providerQuery)
            {
                throw new ArgumentException("The mail cursor belongs to a different query.", nameof(request));
            }

            if (request.AccountIds is { Count: > 0 } &&
                !state.AccountIds.SequenceEqual(request.AccountIds.Distinct(StringComparer.Ordinal), StringComparer.Ordinal))
            {
                throw new ArgumentException("The mail cursor belongs to a different account scope.", nameof(request));
            }

            selectedAccounts = SelectMailAccounts(allAccounts, state.AccountIds);
        }

        var accountsById = selectedAccounts.ToDictionary(account => account.Id, StringComparer.Ordinal);
        foreach (var accountId in state.AccountIds)
        {
            var accountState = state.Accounts[accountId];
            if (accountState.Items.Count > 0 || accountState.Started && accountState.NextProviderCursor is null)
            {
                continue;
            }

            var account = accountsById[accountId];
            if (!_mailReaders.TryGetValue(account.Provider, out var reader))
            {
                accountState.Started = true;
                accountState.Failure = "Mail reading is unavailable for this provider.";
                continue;
            }

            try
            {
                var page = await reader.SearchAsync(
                    account,
                    providerQuery,
                    request.Limit,
                    accountState.Started ? accountState.NextProviderCursor : null,
                    cancellationToken);
                accountState.Started = true;
                accountState.NextProviderCursor = page.NextCursor;
                accountState.Items.AddRange(page.Items);
                accountState.Failure = null;
            }
            catch (ProviderReadException)
            {
                accountState.Started = true;
                accountState.Failure = "Provider read failed. Reconnect this account or try again.";
            }
        }

        var candidates = state.Accounts
            .SelectMany(pair => pair.Value.Items.Select(item => new MailCandidate(pair.Key, item)))
            .OrderByDescending(candidate => candidate.Item.ReceivedAt)
            .ThenBy(candidate => candidate.AccountId, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Item.ProviderMessageId, StringComparer.Ordinal)
            .Take(request.Limit)
            .ToArray();

        var items = new List<MailSearchItem>(candidates.Length);
        foreach (var candidate in candidates)
        {
            state.Accounts[candidate.AccountId].Items.Remove(candidate.Item);
            var reference = _references.Put(
                "m_",
                new MailItemAddress(candidate.AccountId, candidate.Item.ProviderMessageId));
            items.Add(new MailSearchItem(
                reference,
                candidate.AccountId,
                Compact(candidate.Item.Subject, 300),
                Compact(candidate.Item.Sender, 200),
                candidate.Item.ReceivedAt,
                Compact(candidate.Item.Preview, 160)));
        }

        var hasMore = state.Accounts.Values.Any(account =>
            account.Items.Count > 0 || account.NextProviderCursor is not null);
        var nextCursor = hasMore ? _references.Put("c_", state.Copy()) : null;
        var failures = state.Accounts
            .Where(pair => pair.Value.Failure is not null)
            .Select(pair => new AccountReadFailure(pair.Key, pair.Value.Failure!))
            .OrderBy(failure => failure.AccountId, StringComparer.Ordinal)
            .ToArray();

        return new MailSearchResult(
            items,
            state.AccountIds,
            failures,
            failures.Length == 0,
            nextCursor);
    }

    /// <inheritdoc />
    public async Task<MailMessageResult> ReadMailAsync(MailReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reference);
        if (request.Reference.Length > 128 || request.Offset < 0 || request.MaxCharacters is < 1 or > 16_000)
        {
            throw new ArgumentException("The mail reference or text window is invalid.", nameof(request));
        }

        var address = _references.Get<MailItemAddress>(request.Reference, "m_");
        var account = (await _accounts.ListAsync(cancellationToken))
            .SingleOrDefault(item => string.Equals(item.Id, address.AccountId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("The source account is no longer connected.");
        if (!account.MailReadEnabled || !_mailReaders.TryGetValue(account.Provider, out var reader))
        {
            throw new InvalidOperationException("Mail reading is unavailable for the source account.");
        }

        var message = await reader.ReadAsync(account, address.ProviderMessageId, cancellationToken);
        var offset = Math.Min(request.Offset, message.PlainText.Length);
        var length = Math.Min(request.MaxCharacters, message.PlainText.Length - offset);
        var text = message.PlainText.Substring(offset, length);
        return new MailMessageResult(
            request.Reference,
            account.Id,
            Compact(message.Subject, 300),
            Compact(message.Sender, 200),
            message.To.Take(20).Select(value => Compact(value, 200)).ToArray(),
            message.Cc.Take(20).Select(value => Compact(value, 200)).ToArray(),
            message.ReceivedAt,
            text,
            offset,
            offset + length < message.PlainText.Length);
    }

    /// <inheritdoc />
    public async Task<CalendarListResult> ListCalendarsAsync(
        CalendarListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var accounts = SelectCalendarAccounts(await _accounts.ListAsync(cancellationToken), request.AccountIds);
        var calendars = new List<CalendarListItem>();
        var failures = new List<AccountReadFailure>();
        foreach (var account in accounts)
        {
            if (!_calendarReaders.TryGetValue(account.Provider, out var reader))
            {
                failures.Add(new AccountReadFailure(account.Id, "Calendar reading is unavailable for this provider."));
                continue;
            }

            try
            {
                foreach (var calendar in (await reader.ListCalendarsAsync(account, cancellationToken)).Take(100))
                {
                    var reference = _references.Put(
                        "cal_",
                        new CalendarAddress(account.Id, calendar.ProviderCalendarId));
                    calendars.Add(new CalendarListItem(
                        reference,
                        account.Id,
                        Compact(calendar.Name, 200),
                        calendar.Primary,
                        CompactNullable(calendar.TimeZone, 100)));
                }
            }
            catch (ProviderReadException)
            {
                failures.Add(new AccountReadFailure(
                    account.Id,
                    "Provider read failed. Reconnect this account or try again."));
            }
        }

        return new CalendarListResult(
            calendars
                .OrderBy(calendar => calendar.AccountId, StringComparer.Ordinal)
                .ThenByDescending(calendar => calendar.Primary)
                .ThenBy(calendar => calendar.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            accounts.Select(account => account.Id).ToArray(),
            failures.OrderBy(failure => failure.AccountId, StringComparer.Ordinal).ToArray(),
            failures.Count == 0);
    }

    /// <inheritdoc />
    public async Task<EventSearchResult> SearchEventsAsync(
        EventSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var start = ParseExplicitDateTime(request.Start, nameof(request.Start));
        var end = ParseExplicitDateTime(request.End, nameof(request.End));
        if (end <= start || end - start > TimeSpan.FromDays(31))
        {
            throw new ArgumentException("The event window must be positive and no longer than 31 days.", nameof(request));
        }

        if (request.Limit is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Event search limit must be between 1 and 50.");
        }

        var allAccounts = await _accounts.ListAsync(cancellationToken);
        var scopeKey = CreateEventScopeKey(request);
        EventCursorState state;
        if (string.IsNullOrWhiteSpace(request.Cursor))
        {
            state = await CreateEventCursorStateAsync(request, start, end, scopeKey, allAccounts, cancellationToken);
        }
        else
        {
            if (request.Cursor.Length > 128)
            {
                throw new ArgumentException("The event cursor is invalid.", nameof(request));
            }

            state = _references.Get<EventCursorState>(request.Cursor, "ec_").Copy();
            if (state.Start != start || state.End != end || !string.Equals(state.ScopeKey, scopeKey, StringComparison.Ordinal))
            {
                throw new ArgumentException("The event cursor belongs to a different time window or calendar scope.", nameof(request));
            }
        }

        var accountsById = allAccounts.ToDictionary(account => account.Id, StringComparer.Ordinal);
        foreach (var target in state.Targets.Values)
        {
            if (target.Events.Count > 0 || target.Started && target.NextProviderCursor is null)
            {
                continue;
            }

            if (!accountsById.TryGetValue(target.AccountId, out var account) || !account.CalendarReadEnabled)
            {
                target.Started = true;
                target.Failure = "The source account is no longer available for calendar reads.";
                continue;
            }

            if (!_calendarReaders.TryGetValue(account.Provider, out var reader))
            {
                target.Started = true;
                target.Failure = "Calendar reading is unavailable for this provider.";
                continue;
            }

            try
            {
                var page = await reader.SearchEventsAsync(
                    account,
                    target.ProviderCalendarId,
                    start,
                    end,
                    request.Limit,
                    target.Started ? target.NextProviderCursor : null,
                    cancellationToken);
                target.Started = true;
                target.NextProviderCursor = page.NextCursor;
                target.Events.AddRange(page.Events);
                target.Failure = null;
            }
            catch (ProviderReadException)
            {
                target.Started = true;
                target.Failure = "Provider read failed. Reconnect this account or try again.";
            }
        }

        var candidates = state.Targets
            .SelectMany(pair => pair.Value.Events.Select(item => new EventCandidate(pair.Key, item)))
            .OrderBy(candidate => candidate.Event.SortStart)
            .ThenBy(candidate => candidate.TargetKey, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Event.ProviderEventId, StringComparer.Ordinal)
            .Take(request.Limit)
            .ToArray();
        var events = new List<EventSearchItem>(candidates.Length);
        foreach (var candidate in candidates)
        {
            var target = state.Targets[candidate.TargetKey];
            target.Events.Remove(candidate.Event);
            var reference = _references.Put(
                "evt_",
                new EventAddress(target.AccountId, target.ProviderCalendarId, candidate.Event.ProviderEventId, target.CalendarReference));
            events.Add(new EventSearchItem(
                reference,
                target.CalendarReference,
                target.AccountId,
                Compact(candidate.Event.Title, 300),
                candidate.Event.Start,
                candidate.Event.End,
                candidate.Event.AllDay,
                candidate.Event.Cancelled,
                Compact(candidate.Event.Location, 200)));
        }

        var hasMore = state.Targets.Values.Any(target =>
            target.Events.Count > 0 || target.NextProviderCursor is not null);
        var nextCursor = hasMore ? _references.Put("ec_", state.Copy()) : null;
        var failures = state.InitialFailures
            .Concat(state.Targets.Values
                .Where(target => target.Failure is not null)
                .Select(target => new AccountReadFailure(target.AccountId, target.Failure!)))
            .GroupBy(failure => failure.AccountId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(failure => failure.AccountId, StringComparer.Ordinal)
            .ToArray();
        return new EventSearchResult(
            events,
            state.AccountIds,
            failures,
            failures.Length == 0,
            nextCursor);
    }

    /// <inheritdoc />
    public async Task<EventResult> ReadEventAsync(EventReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reference);
        if (request.Reference.Length > 128 || request.MaxDescriptionCharacters is < 1 or > 16_000)
        {
            throw new ArgumentException("The event reference or description limit is invalid.", nameof(request));
        }

        var address = _references.Get<EventAddress>(request.Reference, "evt_");
        var account = (await _accounts.ListAsync(cancellationToken))
            .SingleOrDefault(item => string.Equals(item.Id, address.AccountId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("The source account is no longer connected.");
        if (!account.CalendarReadEnabled || !_calendarReaders.TryGetValue(account.Provider, out var reader))
        {
            throw new InvalidOperationException("Calendar reading is unavailable for the source account.");
        }

        var providerEvent = await reader.ReadEventAsync(
            account,
            address.ProviderCalendarId,
            address.ProviderEventId,
            cancellationToken);
        var descriptionLength = Math.Min(providerEvent.Description.Length, request.MaxDescriptionCharacters);
        return new EventResult(
            request.Reference,
            address.CalendarReference,
            account.Id,
            Compact(providerEvent.Title, 300),
            providerEvent.Start,
            providerEvent.End,
            providerEvent.AllDay,
            providerEvent.Cancelled,
            Compact(providerEvent.Location, 200),
            providerEvent.Description[..descriptionLength],
            descriptionLength < providerEvent.Description.Length,
            providerEvent.Attendees.Take(20).Select(attendee => Compact(attendee, 200)).ToArray(),
            CompactNullable(providerEvent.MeetingLink, 500));
    }

    private async Task<EventCursorState> CreateEventCursorStateAsync(
        EventSearchRequest request,
        DateTimeOffset start,
        DateTimeOffset end,
        string scopeKey,
        IReadOnlyList<Account> allAccounts,
        CancellationToken cancellationToken)
    {
        var targets = new Dictionary<string, CalendarTargetCursorState>(StringComparer.Ordinal);
        var failures = new List<AccountReadFailure>();
        IReadOnlyList<string> accountIds;
        if (request.CalendarReferences is { Count: > 0 })
        {
            if (request.CalendarReferences.Count > 20)
            {
                throw new ArgumentException("Select at most 20 calendars per event search.", nameof(request));
            }

            var selected = request.CalendarReferences.Distinct(StringComparer.Ordinal).ToArray();
            var byId = allAccounts.ToDictionary(account => account.Id, StringComparer.Ordinal);
            var targetIndex = 0;
            foreach (var reference in selected)
            {
                if (reference.Length > 128)
                {
                    throw new ArgumentException("A selected calendar reference is invalid.", nameof(request));
                }

                var address = _references.Get<CalendarAddress>(reference, "cal_");
                if (!byId.TryGetValue(address.AccountId, out var account) || !account.CalendarReadEnabled)
                {
                    throw new ArgumentException("A selected calendar account is unavailable.", nameof(request));
                }

                targets[$"t{targetIndex++}"] = new CalendarTargetCursorState(
                    account.Id,
                    address.ProviderCalendarId,
                    reference);
            }

            accountIds = targets.Values.Select(target => target.AccountId).Distinct(StringComparer.Ordinal).ToArray();
        }
        else
        {
            var accounts = SelectCalendarAccounts(allAccounts, request.AccountIds);
            accountIds = accounts.Select(account => account.Id).ToArray();
            var targetIndex = 0;
            foreach (var account in accounts)
            {
                if (!_calendarReaders.TryGetValue(account.Provider, out var reader))
                {
                    failures.Add(new AccountReadFailure(account.Id, "Calendar reading is unavailable for this provider."));
                    continue;
                }

                try
                {
                    var calendars = await reader.ListCalendarsAsync(account, cancellationToken);
                    var calendar = calendars.FirstOrDefault(item => item.Primary) ?? calendars.FirstOrDefault();
                    if (calendar is null)
                    {
                        failures.Add(new AccountReadFailure(account.Id, "No readable calendar is available."));
                        continue;
                    }

                    var reference = _references.Put(
                        "cal_",
                        new CalendarAddress(account.Id, calendar.ProviderCalendarId));
                    targets[$"t{targetIndex++}"] = new CalendarTargetCursorState(
                        account.Id,
                        calendar.ProviderCalendarId,
                        reference);
                }
                catch (ProviderReadException)
                {
                    failures.Add(new AccountReadFailure(
                        account.Id,
                        "Provider read failed. Reconnect this account or try again."));
                }
            }
        }

        return new EventCursorState(start, end, scopeKey, accountIds, targets, failures);
    }

    private static string CreateEventScopeKey(EventSearchRequest request)
    {
        if (request.CalendarReferences is { Count: > 0 })
        {
            return "cal:" + string.Join(
                '\u001f',
                request.CalendarReferences.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        }

        return request.AccountIds is { Count: > 0 }
            ? "acc:" + string.Join(
                '\u001f',
                request.AccountIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            : "acc:*";
    }

    private static IReadOnlyList<Account> SelectCalendarAccounts(
        IReadOnlyList<Account> allAccounts,
        IReadOnlyList<string>? requestedIds)
    {
        if (requestedIds is null || requestedIds.Count == 0)
        {
            return allAccounts.Where(account => account.CalendarReadEnabled).ToArray();
        }

        var requested = requestedIds.Distinct(StringComparer.Ordinal).ToArray();
        var byId = allAccounts.ToDictionary(account => account.Id, StringComparer.Ordinal);
        var selected = new List<Account>(requested.Length);
        foreach (var id in requested)
        {
            if (!byId.TryGetValue(id, out var account))
            {
                throw new ArgumentException($"Calendar account '{id}' is not connected.", nameof(requestedIds));
            }

            if (!account.CalendarReadEnabled)
            {
                throw new ArgumentException($"Calendar account '{id}' has no calendar read consent.", nameof(requestedIds));
            }

            selected.Add(account);
        }

        return selected;
    }

    private static DateTimeOffset ParseExplicitDateTime(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var timeMarker = value.IndexOf('T');
        var hasOffset = value.EndsWith('Z') ||
                        timeMarker >= 0 && (value.LastIndexOf('+') > timeMarker || value.LastIndexOf('-') > timeMarker);
        if (!hasOffset ||
            !DateTimeOffset.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var result))
        {
            throw new ArgumentException("Event times must be ISO 8601 values with an explicit offset.", parameterName);
        }

        return result;
    }

    private static DateTimeOffset? ParseOptionalExplicitDateTime(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseExplicitDateTime(value, parameterName);

    private static IReadOnlyList<Account> SelectMailAccounts(
        IReadOnlyList<Account> allAccounts,
        IReadOnlyList<string>? requestedIds)
    {
        if (requestedIds is null || requestedIds.Count == 0)
        {
            return allAccounts.Where(account => account.MailReadEnabled).ToArray();
        }

        var requested = requestedIds.Distinct(StringComparer.Ordinal).ToArray();
        var byId = allAccounts.ToDictionary(account => account.Id, StringComparer.Ordinal);
        var selected = new List<Account>(requested.Length);
        foreach (var id in requested)
        {
            if (!byId.TryGetValue(id, out var account))
            {
                throw new ArgumentException($"Mail account '{id}' is not connected.", nameof(requestedIds));
            }

            if (!account.MailReadEnabled)
            {
                throw new ArgumentException($"Mail account '{id}' has no mail read consent.", nameof(requestedIds));
            }

            selected.Add(account);
        }

        return selected;
    }

    private static string Compact(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var compact = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= maximumLength ? compact : compact[..maximumLength];
    }

    private static string? CompactNullable(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Compact(value, maximumLength);

    private sealed record MailCandidate(string AccountId, ProviderMailSummary Item);
    private sealed record MailItemAddress(string AccountId, string ProviderMessageId);
    private sealed record CalendarAddress(string AccountId, string ProviderCalendarId);
    private sealed record EventAddress(
        string AccountId,
        string ProviderCalendarId,
        string ProviderEventId,
        string CalendarReference);
    private sealed record EventCandidate(string TargetKey, ProviderEventSummary Event);

    private sealed record MailCursorState(
        ProviderMailQuery Query,
        IReadOnlyList<string> AccountIds,
        Dictionary<string, AccountMailCursorState> Accounts)
    {
        public MailCursorState Copy() => new(
            Query,
            AccountIds.ToArray(),
            Accounts.ToDictionary(pair => pair.Key, pair => pair.Value.Copy(), StringComparer.Ordinal));
    }

    private sealed class AccountMailCursorState
    {
        public bool Started { get; set; }
        public string? NextProviderCursor { get; set; }
        public string? Failure { get; set; }
        public List<ProviderMailSummary> Items { get; } = [];

        public AccountMailCursorState Copy()
        {
            var clone = new AccountMailCursorState
            {
                Started = Started,
                NextProviderCursor = NextProviderCursor,
                Failure = Failure
            };
            clone.Items.AddRange(Items);
            return clone;
        }
    }

    private sealed record EventCursorState(
        DateTimeOffset Start,
        DateTimeOffset End,
        string ScopeKey,
        IReadOnlyList<string> AccountIds,
        Dictionary<string, CalendarTargetCursorState> Targets,
        IReadOnlyList<AccountReadFailure> InitialFailures)
    {
        public EventCursorState Copy() => new(
            Start,
            End,
            ScopeKey,
            AccountIds.ToArray(),
            Targets.ToDictionary(pair => pair.Key, pair => pair.Value.Copy(), StringComparer.Ordinal),
            InitialFailures.ToArray());
    }

    private sealed class CalendarTargetCursorState(
        string accountId,
        string providerCalendarId,
        string calendarReference)
    {
        public string AccountId { get; } = accountId;
        public string ProviderCalendarId { get; } = providerCalendarId;
        public string CalendarReference { get; } = calendarReference;
        public bool Started { get; set; }
        public string? NextProviderCursor { get; set; }
        public string? Failure { get; set; }
        public List<ProviderEventSummary> Events { get; } = [];

        public CalendarTargetCursorState Copy()
        {
            var clone = new CalendarTargetCursorState(AccountId, ProviderCalendarId, CalendarReference)
            {
                Started = Started,
                NextProviderCursor = NextProviderCursor,
                Failure = Failure
            };
            clone.Events.AddRange(Events);
            return clone;
        }
    }

    private sealed class LocalReferenceStore
    {
        private const int MaximumEntries = 4_096;
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
        private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

        public string Put<T>(string prefix, T value) where T : notnull
        {
            Prune();
            var reference = prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(12))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            _entries[reference] = new Entry(value, DateTimeOffset.UtcNow.Add(Lifetime));
            return reference;
        }

        public T Get<T>(string reference, string prefix)
        {
            if (!reference.StartsWith(prefix, StringComparison.Ordinal) ||
                !_entries.TryGetValue(reference, out var entry) ||
                entry.ExpiresAt <= DateTimeOffset.UtcNow ||
                entry.Value is not T value)
            {
                _entries.TryRemove(reference, out _);
                throw new ArgumentException("The local result reference is invalid or expired.", nameof(reference));
            }

            return value;
        }

        private void Prune()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var pair in _entries.Where(pair => pair.Value.ExpiresAt <= now))
            {
                _entries.TryRemove(pair.Key, out _);
            }

            if (_entries.Count < MaximumEntries)
            {
                return;
            }

            foreach (var pair in _entries.OrderBy(pair => pair.Value.ExpiresAt).Take(_entries.Count - MaximumEntries + 1))
            {
                _entries.TryRemove(pair.Key, out _);
            }
        }

        private sealed record Entry(object Value, DateTimeOffset ExpiresAt);
    }
}
