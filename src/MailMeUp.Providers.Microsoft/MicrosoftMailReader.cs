using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using MailMeUp.Core;
using MailMeUp.Security;

namespace MailMeUp.Providers.Microsoft;

/// <summary>Reads Microsoft Graph mail search results and selected messages without changing mailbox state.</summary>
public sealed class MicrosoftMailReader : IMailReader
{
    private const int MaximumJsonBytes = 12 * 1024 * 1024;
    private static readonly HttpClient HttpClient = new();
    private readonly MicrosoftAccessTokenProvider _tokens;

    /// <summary>Creates a Microsoft mail reader backed by the protected MSAL cache.</summary>
    public MicrosoftMailReader(IProviderConfigurationStore configurations, ISecretStore secrets)
    {
        _tokens = new MicrosoftAccessTokenProvider(configurations, secrets);
    }

    /// <inheritdoc />
    public string ProviderId => "microsoft";

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
        if (limit is < 1 or > 50)
        {
            throw new ArgumentException("The Microsoft mail search page is invalid.");
        }

        try
        {
            var accessToken = await _tokens.GetAsync(account, ["Mail.Read"], cancellationToken);
            var url = string.IsNullOrWhiteSpace(cursor) ? CreateSearchUrl(query, limit) : ValidateNextLink(cursor);
            using var document = await GetJsonAsync(url, accessToken, preferText: false, cancellationToken);
            var summaries = new List<ProviderMailSummary>();
            if (document.RootElement.TryGetProperty("value", out var values) && values.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in values.EnumerateArray().Take(limit))
                {
                    var id = GetOptionalString(item, "id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    var summary = new ProviderMailSummary(
                        id,
                        GetOptionalString(item, "subject") ?? "(no subject)",
                        ReadSender(item),
                        ReadDate(item),
                        GetOptionalString(item, "bodyPreview") ?? string.Empty);
                    if ((query.Start is null || summary.ReceivedAt >= query.Start.Value) &&
                        (query.End is null || summary.ReceivedAt < query.End.Value))
                    {
                        summaries.Add(summary);
                    }
                }
            }

            return new ProviderMailSearchPage(
                summaries,
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
            throw new ProviderReadException("Microsoft mail search failed.");
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
            var accessToken = await _tokens.GetAsync(account, ["Mail.Read"], cancellationToken);
            var url = $"https://graph.microsoft.com/v1.0/me/messages/{Uri.EscapeDataString(providerMessageId)}" +
                      "?%24select=id%2Csubject%2Cfrom%2CtoRecipients%2CccRecipients%2CreceivedDateTime%2Cbody";
            using var document = await GetJsonAsync(url, accessToken, preferText: true, cancellationToken);
            var root = document.RootElement;
            var body = root.TryGetProperty("body", out var bodyProperty)
                ? GetOptionalString(bodyProperty, "content") ?? string.Empty
                : string.Empty;
            var contentType = root.TryGetProperty("body", out bodyProperty)
                ? GetOptionalString(bodyProperty, "contentType")
                : null;
            if (string.Equals(contentType, "html", StringComparison.OrdinalIgnoreCase))
            {
                body = HtmlToText(body);
            }

            return new ProviderMailMessage(
                providerMessageId,
                GetOptionalString(root, "subject") ?? "(no subject)",
                ReadSender(root),
                ReadRecipients(root, "toRecipients"),
                ReadRecipients(root, "ccRecipients"),
                ReadDate(root),
                body);
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
            throw new ProviderReadException("The Microsoft message could not be read.");
        }
    }

    private static string CreateSearchUrl(ProviderMailQuery query, int limit)
    {
        var parts = new List<string> { query.Text };
        if (!string.IsNullOrWhiteSpace(query.Sender))
        {
            parts.Add($"from:\"{query.Sender}\"");
        }

        if (query.Start is not null)
        {
            parts.Add($"received>={query.Start.Value.UtcDateTime:yyyy-MM-dd}");
        }

        if (query.End is not null)
        {
            parts.Add($"received<{query.End.Value.UtcDateTime:yyyy-MM-dd}");
        }

        var providerQuery = string.Join(" AND ", parts);
        var escapedSearch = providerQuery.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return "https://graph.microsoft.com/v1.0/me/messages?" +
               "%24search=" + Uri.EscapeDataString($"\"{escapedSearch}\"") +
               "&%24select=id%2Csubject%2Cfrom%2CreceivedDateTime%2CbodyPreview" +
               "&%24top=" + limit.ToString(CultureInfo.InvariantCulture);
    }

    private static string ValidateNextLink(string cursor)
    {
        if (cursor.Length > 8_192 ||
            !Uri.TryCreate(cursor, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "graph.microsoft.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith("/v1.0/me/messages", StringComparison.Ordinal))
        {
            throw new ProviderReadException("The Microsoft mail continuation is invalid.");
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
        request.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");
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

    private static string ReadSender(JsonElement message)
    {
        if (!message.TryGetProperty("from", out var from) ||
            !from.TryGetProperty("emailAddress", out var emailAddress))
        {
            return string.Empty;
        }

        return FormatAddress(emailAddress);
    }

    private static IReadOnlyList<string> ReadRecipients(JsonElement message, string propertyName)
    {
        if (!message.TryGetProperty(propertyName, out var recipients) || recipients.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return recipients.EnumerateArray()
            .Select(recipient => recipient.TryGetProperty("emailAddress", out var address) ? FormatAddress(address) : string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Take(20)
            .ToArray();
    }

    private static string FormatAddress(JsonElement emailAddress)
    {
        var name = GetOptionalString(emailAddress, "name");
        var address = GetOptionalString(emailAddress, "address");
        if (string.IsNullOrWhiteSpace(name))
        {
            return address ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(address) ? name : $"{name} <{address}>";
    }

    private static DateTimeOffset ReadDate(JsonElement message)
    {
        var value = GetOptionalString(message, "receivedDateTime");
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result)
            ? result
            : DateTimeOffset.UnixEpoch;
    }

    private static string? GetOptionalString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

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

    private static void ValidateMailAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!string.Equals(account.Provider, "microsoft", StringComparison.Ordinal) || !account.MailReadEnabled)
        {
            throw new ArgumentException("The account has no Microsoft mail read access.", nameof(account));
        }
    }

    private static void ValidateMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        if (messageId.Length > 1_024 || messageId.Any(char.IsControl))
        {
            throw new ArgumentException("The Microsoft message identifier is invalid.", nameof(messageId));
        }
    }
}
