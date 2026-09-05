using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MailMeUp.Core;
using MailMeUp.Security;

namespace MailMeUp.Providers.Google;

/// <summary>Reads Gmail search results and selected messages without changing mailbox state.</summary>
public sealed class GoogleMailReader : IMailReader
{
    private const int MaximumJsonBytes = 12 * 1024 * 1024;
    private static readonly HttpClient HttpClient = new();
    private readonly GoogleAccessTokenProvider _tokens;

    /// <summary>Creates a Gmail reader backed by protected Google account tokens.</summary>
    public GoogleMailReader(IProviderConfigurationStore configurations, ISecretStore secrets)
    {
        _tokens = new GoogleAccessTokenProvider(configurations, secrets);
    }

    /// <inheritdoc />
    public string ProviderId => "google";

    /// <inheritdoc />
    public async Task<ProviderMailSearchPage> SearchAsync(
        Account account,
        ProviderMailQuery query,
        int limit,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        ValidateMailAccount(account);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Text);
        if (limit is < 1 or > 50 || cursor is { Length: > 4_096 })
        {
            throw new ArgumentException("The Gmail search page is invalid.");
        }

        try
        {
            var accessToken = await _tokens.GetAsync(account, cancellationToken);
            var url = new StringBuilder("https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults=")
                .Append(limit.ToString(CultureInfo.InvariantCulture))
                .Append("&q=").Append(Uri.EscapeDataString(CreateProviderQuery(query)))
                .Append("&fields=messages(id%2CthreadId)%2CnextPageToken")
                .ToString();
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                url += "&pageToken=" + Uri.EscapeDataString(cursor);
            }

            using var page = await GetJsonAsync(url, accessToken, cancellationToken);
            var summaries = new List<ProviderMailSummary>();
            if (page.RootElement.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in messages.EnumerateArray().Take(limit))
                {
                    if (!item.TryGetProperty("id", out var idProperty) || idProperty.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var id = idProperty.GetString();
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    summaries.Add(await ReadSummaryAsync(id, accessToken, cancellationToken));
                }
            }

            var next = GetOptionalString(page.RootElement, "nextPageToken");
            return new ProviderMailSearchPage(summaries, next);
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
            throw new ProviderReadException("Gmail search failed.");
        }
    }

    /// <inheritdoc />
    public async Task<ProviderMailMessage> ReadAsync(
        Account account,
        string providerMessageId,
        CancellationToken cancellationToken = default)
    {
        ValidateMailAccount(account);
        ValidateMessageId(providerMessageId);
        try
        {
            var accessToken = await _tokens.GetAsync(account, cancellationToken);
            var url = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{Uri.EscapeDataString(providerMessageId)}?format=full";
            using var document = await GetJsonAsync(url, accessToken, cancellationToken);
            var root = document.RootElement;
            var headers = root.TryGetProperty("payload", out var payload)
                ? ReadHeaders(payload)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var text = root.TryGetProperty("payload", out payload) ? ReadBody(payload) : string.Empty;
            return new ProviderMailMessage(
                providerMessageId,
                GetHeader(headers, "Subject", "(no subject)"),
                GetHeader(headers, "From", string.Empty),
                AsHeaderList(headers, "To"),
                AsHeaderList(headers, "Cc"),
                ReadInternalDate(root),
                text);
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
            throw new ProviderReadException("The Gmail message could not be read.");
        }
    }

    private static async Task<ProviderMailSummary> ReadSummaryAsync(
        string messageId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        ValidateMessageId(messageId);
        var url = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{Uri.EscapeDataString(messageId)}" +
                  "?format=metadata&metadataHeaders=Subject&metadataHeaders=From" +
                  "&fields=id%2CinternalDate%2Csnippet%2Cpayload%2Fheaders";
        using var document = await GetJsonAsync(url, accessToken, cancellationToken);
        var root = document.RootElement;
        var headers = root.TryGetProperty("payload", out var payload)
            ? ReadHeaders(payload)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return new ProviderMailSummary(
            messageId,
            GetHeader(headers, "Subject", "(no subject)"),
            GetHeader(headers, "From", string.Empty),
            ReadInternalDate(root),
            GetOptionalString(root, "snippet") ?? string.Empty);
    }

    private static string CreateProviderQuery(ProviderMailQuery query)
    {
        var parts = new List<string> { query.Text };
        if (!string.IsNullOrWhiteSpace(query.Sender))
        {
            parts.Add($"from:\"{query.Sender.Replace("\"", string.Empty, StringComparison.Ordinal)}\"");
        }

        if (query.Start is not null)
        {
            parts.Add($"after:{query.Start.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}");
        }

        if (query.End is not null)
        {
            parts.Add($"before:{query.End.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}");
        }

        return string.Join(' ', parts);
    }

    private static async Task<JsonDocument> GetJsonAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderReadException("Gmail returned an unsuccessful response.");
        }

        if (response.Content.Headers.ContentLength is > MaximumJsonBytes)
        {
            throw new ProviderReadException("The Gmail response is too large.");
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
                throw new ProviderReadException("The Gmail response is too large.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        buffer.Position = 0;
        return await JsonDocument.ParseAsync(buffer, cancellationToken: cancellationToken);
    }

    private static Dictionary<string, string> ReadHeaders(JsonElement payload)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!payload.TryGetProperty("headers", out var headers) || headers.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var header in headers.EnumerateArray())
        {
            var name = GetOptionalString(header, "name");
            var value = GetOptionalString(header, "value");
            if (!string.IsNullOrWhiteSpace(name) && value is not null)
            {
                result[name] = value;
            }
        }

        return result;
    }

    private static string ReadBody(JsonElement payload)
    {
        var plain = new List<string>();
        var html = new List<string>();
        CollectBodies(payload, plain, html);
        if (plain.Count > 0)
        {
            return string.Join("\n\n", plain.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        return html.Count == 0 ? string.Empty : HtmlToText(string.Join("\n", html));
    }

    private static void CollectBodies(JsonElement part, List<string> plain, List<string> html)
    {
        var mimeType = GetOptionalString(part, "mimeType");
        if (part.TryGetProperty("body", out var body))
        {
            var data = GetOptionalString(body, "data");
            if (!string.IsNullOrWhiteSpace(data))
            {
                var decoded = DecodeBase64Url(data);
                if (string.Equals(mimeType, "text/plain", StringComparison.OrdinalIgnoreCase))
                {
                    plain.Add(decoded);
                }
                else if (string.Equals(mimeType, "text/html", StringComparison.OrdinalIgnoreCase))
                {
                    html.Add(decoded);
                }
            }
        }

        if (part.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in parts.EnumerateArray())
            {
                CollectBodies(child, plain, html);
            }
        }
    }

    private static string DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

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

    private static DateTimeOffset ReadInternalDate(JsonElement root)
    {
        var value = GetOptionalString(root, "internalDate");
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds)
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds)
            : DateTimeOffset.UnixEpoch;
    }

    private static string? GetOptionalString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string GetHeader(IReadOnlyDictionary<string, string> headers, string name, string fallback) =>
        headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static IReadOnlyList<string> AsHeaderList(IReadOnlyDictionary<string, string> headers, string name) =>
        headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? [value] : [];

    private static void ValidateMailAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!string.Equals(account.Provider, "google", StringComparison.Ordinal) || !account.MailReadEnabled)
        {
            throw new ArgumentException("The account has no Gmail read access.", nameof(account));
        }
    }

    private static void ValidateMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        if (messageId.Length > 256 || messageId.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException("The Gmail message identifier is invalid.", nameof(messageId));
        }
    }
}
