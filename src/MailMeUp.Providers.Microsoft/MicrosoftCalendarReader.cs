using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using MailMeUp.Core;
using MailMeUp.Security;

namespace MailMeUp.Providers.Microsoft;

/// <summary>Reads Microsoft calendars and appointments without changing calendar data or attendance.</summary>
public sealed class MicrosoftCalendarReader : ICalendarReader
{
    private const int MaximumJsonBytes = 12 * 1024 * 1024;
    private static readonly HttpClient HttpClient = new();
    private readonly MicrosoftAccessTokenProvider _tokens;

    /// <summary>Creates a Microsoft calendar reader backed by the protected MSAL cache.</summary>
    public MicrosoftCalendarReader(IProviderConfigurationStore configurations, ISecretStore secrets)
    {
        _tokens = new MicrosoftAccessTokenProvider(configurations, secrets);
    }

    /// <inheritdoc />
    public string ProviderId => "microsoft";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProviderCalendar>> ListCalendarsAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        ValidateCalendarAccount(account);
        try
        {
            var accessToken = await _tokens.GetAsync(account, ["Calendars.Read"], cancellationToken);
            var calendars = new List<ProviderCalendar>();
            string? url = "https://graph.microsoft.com/v1.0/me/calendars?%24select=id%2Cname%2CisDefaultCalendar&%24top=100";
            while (url is not null && calendars.Count < 500)
            {
                using var document = await GetJsonAsync(url, accessToken, preferText: false, cancellationToken);
                if (document.RootElement.TryGetProperty("value", out var values) && values.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in values.EnumerateArray())
                    {
                        var id = GetOptionalString(item, "id");
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            calendars.Add(new ProviderCalendar(
                                id,
                                GetOptionalString(item, "name") ?? id,
                                GetOptionalBoolean(item, "isDefaultCalendar"),
                                TimeZone: null));
                        }
                    }
                }

                var next = GetOptionalString(document.RootElement, "@odata.nextLink");
                url = string.IsNullOrWhiteSpace(next) ? null : ValidateNextLink(next, "/v1.0/me/calendars");
            }

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
            throw new ProviderReadException("Microsoft calendars could not be listed.");
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
        if (limit is < 1 or > 50)
        {
            throw new ArgumentException("The Microsoft event search page is invalid.");
        }

        try
        {
            var accessToken = await _tokens.GetAsync(account, ["Calendars.Read"], cancellationToken);
            var url = string.IsNullOrWhiteSpace(cursor)
                ? CreateEventSearchUrl(providerCalendarId, start, end, limit)
                : ValidateNextLink(cursor, "/calendarView");
            using var document = await GetJsonAsync(url, accessToken, preferText: false, cancellationToken);
            var events = new List<ProviderEventSummary>();
            if (document.RootElement.TryGetProperty("value", out var values) && values.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in values.EnumerateArray())
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
                GetOptionalString(document.RootElement, "@odata.nextLink"));
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
            throw new ProviderReadException("Microsoft appointments could not be searched.");
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
            var accessToken = await _tokens.GetAsync(account, ["Calendars.Read"], cancellationToken);
            var url = $"https://graph.microsoft.com/v1.0/me/calendars/{Uri.EscapeDataString(providerCalendarId)}" +
                      $"/events/{Uri.EscapeDataString(providerEventId)}" +
                      "?%24select=id%2Csubject%2Cbody%2Cstart%2Cend%2CisAllDay%2CisCancelled%2Clocation%2Cattendees%2ConlineMeeting%2ConlineMeetingUrl";
            using var document = await GetJsonAsync(url, accessToken, preferText: true, cancellationToken);
            var root = document.RootElement;
            var boundaries = ParseBoundaries(root)
                ?? throw new ProviderReadException("Microsoft returned an appointment without a valid time.");
            var description = root.TryGetProperty("body", out var body)
                ? GetOptionalString(body, "content") ?? string.Empty
                : string.Empty;
            if (root.TryGetProperty("body", out body) &&
                string.Equals(GetOptionalString(body, "contentType"), "html", StringComparison.OrdinalIgnoreCase))
            {
                description = HtmlToText(description);
            }

            return new ProviderEvent(
                providerEventId,
                GetOptionalString(root, "subject") ?? "(untitled appointment)",
                boundaries.Start,
                boundaries.End,
                boundaries.AllDay,
                GetOptionalBoolean(root, "isCancelled"),
                ReadLocation(root),
                description,
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
            throw new ProviderReadException("The Microsoft appointment could not be read.");
        }
    }

    private static string CreateEventSearchUrl(
        string calendarId,
        DateTimeOffset start,
        DateTimeOffset end,
        int limit) =>
        $"https://graph.microsoft.com/v1.0/me/calendars/{Uri.EscapeDataString(calendarId)}/calendarView" +
        "?startDateTime=" + Uri.EscapeDataString(start.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)) +
        "&endDateTime=" + Uri.EscapeDataString(end.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)) +
        "&%24select=id%2Csubject%2Cstart%2Cend%2CisAllDay%2CisCancelled%2Clocation" +
        "&%24orderby=start%2FdateTime&%24top=" + limit.ToString(CultureInfo.InvariantCulture);

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
            GetOptionalString(item, "subject") ?? "(untitled appointment)",
            boundaries.Start,
            boundaries.End,
            boundaries.AllDay,
            GetOptionalBoolean(item, "isCancelled"),
            ReadLocation(item));
    }

    private static EventBoundaries? ParseBoundaries(JsonElement item)
    {
        if (!item.TryGetProperty("start", out var startElement) ||
            !item.TryGetProperty("end", out var endElement))
        {
            return null;
        }

        var startValue = GetOptionalString(startElement, "dateTime");
        var endValue = GetOptionalString(endElement, "dateTime");
        if (!TryParseGraphDate(startValue, out var start) || !TryParseGraphDate(endValue, out var end))
        {
            return null;
        }

        var allDay = GetOptionalBoolean(item, "isAllDay");
        return allDay
            ? new EventBoundaries(
                start,
                start.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                end.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                AllDay: true)
            : new EventBoundaries(
                start,
                start.ToString("O", CultureInfo.InvariantCulture),
                end.ToString("O", CultureInfo.InvariantCulture),
                AllDay: false);
    }

    private static bool TryParseGraphDate(string? value, out DateTimeOffset result)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static string ReadLocation(JsonElement item)
    {
        if (!item.TryGetProperty("location", out var location))
        {
            return string.Empty;
        }

        return GetOptionalString(location, "displayName") ?? string.Empty;
    }

    private static IReadOnlyList<string> ReadAttendees(JsonElement item)
    {
        if (!item.TryGetProperty("attendees", out var attendees) || attendees.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return attendees.EnumerateArray().Select(attendee =>
        {
            if (!attendee.TryGetProperty("emailAddress", out var emailAddress))
            {
                return string.Empty;
            }

            var name = GetOptionalString(emailAddress, "name");
            var address = GetOptionalString(emailAddress, "address");
            var identity = string.IsNullOrWhiteSpace(name)
                ? address
                : string.IsNullOrWhiteSpace(address) ? name : $"{name} <{address}>";
            var response = attendee.TryGetProperty("status", out var status)
                ? GetOptionalString(status, "response")
                : null;
            return string.IsNullOrWhiteSpace(identity)
                ? string.Empty
                : string.IsNullOrWhiteSpace(response) ? identity : $"{identity} [{response}]";
        }).Where(value => !string.IsNullOrWhiteSpace(value)).Take(20).ToArray();
    }

    private static string? ReadMeetingLink(JsonElement item)
    {
        if (item.TryGetProperty("onlineMeeting", out var onlineMeeting))
        {
            var joinUrl = GetOptionalString(onlineMeeting, "joinUrl");
            if (!string.IsNullOrWhiteSpace(joinUrl))
            {
                return joinUrl;
            }
        }

        return GetOptionalString(item, "onlineMeetingUrl");
    }

    private static string ValidateNextLink(string cursor, string requiredPathPart)
    {
        if (cursor.Length > 8_192 ||
            !Uri.TryCreate(cursor, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "graph.microsoft.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.Contains(requiredPathPart, StringComparison.Ordinal))
        {
            throw new ProviderReadException("The Microsoft calendar continuation is invalid.");
        }

        return uri.AbsoluteUri;
    }

    private static async Task<JsonDocument> GetJsonAsync(
        string url,
        string accessToken,
        bool preferText,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("Prefer", "outlook.timezone=\"UTC\"");
        if (preferText)
        {
            request.Headers.TryAddWithoutValidation("Prefer", "outlook.body-content-type=\"text\"");
        }

        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderReadException("Microsoft Graph returned an unsuccessful response.");
        }

        if (response.Content.Headers.ContentLength is > MaximumJsonBytes)
        {
            throw new ProviderReadException("The Microsoft Graph response is too large.");
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
                throw new ProviderReadException("The Microsoft Graph response is too large.");
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
        if (!string.Equals(account.Provider, "microsoft", StringComparison.Ordinal) || !account.CalendarReadEnabled)
        {
            throw new ArgumentException("The account has no Microsoft calendar read access.", nameof(account));
        }
    }

    private static void ValidateProviderId(string value, string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 1_024 || value.Any(char.IsControl))
        {
            throw new ArgumentException($"The Microsoft {kind} identifier is invalid.", nameof(value));
        }
    }

    private sealed record EventBoundaries(DateTimeOffset SortStart, string Start, string End, bool AllDay);
}
