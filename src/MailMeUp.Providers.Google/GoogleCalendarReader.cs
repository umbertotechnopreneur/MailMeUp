using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using MailMeUp.Core;
using MailMeUp.Security;

namespace MailMeUp.Providers.Google;

/// <summary>Reads Google calendars and appointments without changing calendar data or attendance.</summary>
public sealed class GoogleCalendarReader : ICalendarReader
{
    private const int MaximumJsonBytes = 12 * 1024 * 1024;
    private static readonly HttpClient HttpClient = new();
    private readonly GoogleAccessTokenProvider _tokens;

    /// <summary>Creates a Google Calendar reader backed by protected account tokens.</summary>
    public GoogleCalendarReader(IProviderConfigurationStore configurations, ISecretStore secrets)
    {
        _tokens = new GoogleAccessTokenProvider(configurations, secrets);
    }

    /// <inheritdoc />
    public string ProviderId => "google";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProviderCalendar>> ListCalendarsAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        ValidateCalendarAccount(account);
        try
        {
            var accessToken = await _tokens.GetAsync(account, cancellationToken);
            var calendars = new List<ProviderCalendar>();
            string? cursor = null;
            do
            {
                var url = "https://www.googleapis.com/calendar/v3/users/me/calendarList" +
                          "?maxResults=250&fields=items(id%2Csummary%2Cprimary%2CtimeZone%2CaccessRole)%2CnextPageToken";
                if (!string.IsNullOrWhiteSpace(cursor))
                {
                    url += "&pageToken=" + Uri.EscapeDataString(cursor);
                }

                using var document = await GetJsonAsync(url, accessToken, cancellationToken);
                if (document.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var id = GetOptionalString(item, "id");
                        var role = GetOptionalString(item, "accessRole");
                        if (string.IsNullOrWhiteSpace(id) ||
                            string.Equals(role, "freeBusyReader", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(role, "none", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        calendars.Add(new ProviderCalendar(
                            id,
                            GetOptionalString(item, "summary") ?? id,
                            GetOptionalBoolean(item, "primary"),
                            GetOptionalString(item, "timeZone")));
                    }
                }

                cursor = GetOptionalString(document.RootElement, "nextPageToken");
            }
            while (!string.IsNullOrWhiteSpace(cursor) && calendars.Count < 500);

            return calendars;
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
            throw new ProviderReadException("Google calendars could not be listed.");
        }
    }

    /// <inheritdoc />
    public async Task<ProviderEventSearchPage> SearchEventsAsync(
        Account account,
        string providerCalendarId,
        DateTimeOffset start,
        DateTimeOffset end,
        int limit,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        ValidateCalendarAccount(account);
        ValidateProviderId(providerCalendarId, "calendar");
        if (limit is < 1 or > 50 || cursor is { Length: > 4_096 })
        {
            throw new ArgumentException("The Google event search page is invalid.");
        }

        try
        {
            var accessToken = await _tokens.GetAsync(account, cancellationToken);
            var url = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(providerCalendarId)}/events" +
                      "?singleEvents=true&orderBy=startTime&showDeleted=true" +
                      "&timeMin=" + Uri.EscapeDataString(start.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)) +
                      "&timeMax=" + Uri.EscapeDataString(end.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)) +
                      "&maxResults=" + limit.ToString(CultureInfo.InvariantCulture) +
                      "&fields=items(id%2Cstatus%2Csummary%2Clocation%2Cstart%2Cend)%2CnextPageToken";
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                url += "&pageToken=" + Uri.EscapeDataString(cursor);
            }

            using var document = await GetJsonAsync(url, accessToken, cancellationToken);
            var events = new List<ProviderEventSummary>();
            if (document.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var parsed = ParseEventSummary(item);
                    if (parsed is not null)
                    {
                        events.Add(parsed);
                    }
                }
            }

            return new ProviderEventSearchPage(
                events,
                GetOptionalString(document.RootElement, "nextPageToken"));
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
            throw new ProviderReadException("Google appointments could not be searched.");
        }
    }

    /// <inheritdoc />
    public async Task<ProviderEvent> ReadEventAsync(
        Account account,
        string providerCalendarId,
        string providerEventId,
        CancellationToken cancellationToken = default)
    {
        ValidateCalendarAccount(account);
        ValidateProviderId(providerCalendarId, "calendar");
        ValidateProviderId(providerEventId, "event");
        try
        {
            var accessToken = await _tokens.GetAsync(account, cancellationToken);
            var url = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(providerCalendarId)}" +
                      $"/events/{Uri.EscapeDataString(providerEventId)}" +
                      "?fields=id%2Cstatus%2Csummary%2Cdescription%2Clocation%2Cstart%2Cend%2Cattendees(displayName%2Cemail%2CresponseStatus)%2ChangoutLink%2CconferenceData(entryPoints)";
            using var document = await GetJsonAsync(url, accessToken, cancellationToken);
            var root = document.RootElement;
            var boundaries = ParseBoundaries(root)
                ?? throw new ProviderReadException("Google returned an appointment without a valid time.");
            return new ProviderEvent(
                providerEventId,
                GetOptionalString(root, "summary") ?? "(untitled appointment)",
                boundaries.Start,
                boundaries.End,
                boundaries.AllDay,
                string.Equals(GetOptionalString(root, "status"), "cancelled", StringComparison.OrdinalIgnoreCase),
                GetOptionalString(root, "location") ?? string.Empty,
                HtmlToText(GetOptionalString(root, "description") ?? string.Empty),
                ReadAttendees(root),
                ReadMeetingLink(root));
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
            throw new ProviderReadException("The Google appointment could not be read.");
        }
    }

    private static ProviderEventSummary? ParseEventSummary(JsonElement item)
    {
        var id = GetOptionalString(item, "id");
        var boundaries = ParseBoundaries(item);
        if (string.IsNullOrWhiteSpace(id) || boundaries is null)
        {
            return null;
        }

        return new ProviderEventSummary(
            id,
            boundaries.SortStart,
            GetOptionalString(item, "summary") ?? "(untitled appointment)",
            boundaries.Start,
            boundaries.End,
            boundaries.AllDay,
            string.Equals(GetOptionalString(item, "status"), "cancelled", StringComparison.OrdinalIgnoreCase),
            GetOptionalString(item, "location") ?? string.Empty);
    }

    private static EventBoundaries? ParseBoundaries(JsonElement item)
    {
        if (!item.TryGetProperty("start", out var startElement) ||
            !item.TryGetProperty("end", out var endElement))
        {
            return null;
        }

        var startDateTime = GetOptionalString(startElement, "dateTime");
        var endDateTime = GetOptionalString(endElement, "dateTime");
        if (DateTimeOffset.TryParse(startDateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var start) &&
            DateTimeOffset.TryParse(endDateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        {
            return new EventBoundaries(start, startDateTime!, endDateTime!, AllDay: false);
        }

        var startDate = GetOptionalString(startElement, "date");
        var endDate = GetOptionalString(endElement, "date");
        if (DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var allDayStart) &&
            DateOnly.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return new EventBoundaries(
                new DateTimeOffset(allDayStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                startDate!,
                endDate!,
                AllDay: true);
        }

        return null;
    }

    private static IReadOnlyList<string> ReadAttendees(JsonElement item)
    {
        if (!item.TryGetProperty("attendees", out var attendees) || attendees.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return attendees.EnumerateArray().Select(attendee =>
        {
            var name = GetOptionalString(attendee, "displayName");
            var email = GetOptionalString(attendee, "email");
            var response = GetOptionalString(attendee, "responseStatus");
            var identity = string.IsNullOrWhiteSpace(name) ? email : string.IsNullOrWhiteSpace(email) ? name : $"{name} <{email}>";
            return string.IsNullOrWhiteSpace(identity)
                ? string.Empty
                : string.IsNullOrWhiteSpace(response) ? identity : $"{identity} [{response}]";
        }).Where(value => !string.IsNullOrWhiteSpace(value)).Take(20).ToArray();
    }

    private static string? ReadMeetingLink(JsonElement item)
    {
        var direct = GetOptionalString(item, "hangoutLink");
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (!item.TryGetProperty("conferenceData", out var conference) ||
            !conference.TryGetProperty("entryPoints", out var entryPoints) ||
            entryPoints.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var entry in entryPoints.EnumerateArray())
        {
            if (string.Equals(GetOptionalString(entry, "entryPointType"), "video", StringComparison.OrdinalIgnoreCase))
            {
                return GetOptionalString(entry, "uri");
            }
        }

        return null;
    }

    private static async Task<JsonDocument> GetJsonAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderReadException("Google Calendar returned an unsuccessful response.");
        }

        if (response.Content.Headers.ContentLength is > MaximumJsonBytes)
        {
            throw new ProviderReadException("The Google Calendar response is too large.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[32 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > MaximumJsonBytes)
            {
                throw new ProviderReadException("The Google Calendar response is too large.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        buffer.Position = 0;
        return await JsonDocument.ParseAsync(buffer, cancellationToken: cancellationToken);
    }

    private static string? GetOptionalString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool GetOptionalBoolean(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var property) &&
        property.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        property.GetBoolean();

    private static string HtmlToText(string html)
    {
        var withoutTags = Regex.Replace(
            html,
            "<[^>]+>",
            " ",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        var decoded = WebUtility.HtmlDecode(withoutTags) ?? string.Empty;
        return string.Join(' ', decoded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static void ValidateCalendarAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!string.Equals(account.Provider, "google", StringComparison.Ordinal) || !account.CalendarReadEnabled)
        {
            throw new ArgumentException("The account has no Google Calendar read access.", nameof(account));
        }
    }

    private static void ValidateProviderId(string value, string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 1_024 || value.Any(char.IsControl))
        {
            throw new ArgumentException($"The Google {kind} identifier is invalid.", nameof(value));
        }
    }

    private sealed record EventBoundaries(DateTimeOffset SortStart, string Start, string End, bool AllDay);
}
