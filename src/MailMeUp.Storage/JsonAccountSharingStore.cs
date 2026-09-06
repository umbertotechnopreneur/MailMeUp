using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MailMeUp.Core;

namespace MailMeUp.Storage;

/// <summary>Stores atomic per-account sharing settings, without in-process caching or credential material.</summary>
public sealed class JsonAccountSharingStore(string directory) : IAccountSharingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true
    };

    private readonly string _directory = Path.Combine(DataDirectory.Resolve(directory), "sharing");

    /// <inheritdoc />
    public async Task<AccountSharingSettings?> GetAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(accountId);
        // Opening directly distinguishes an absent legacy setting from access failures, which must fail closed.
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }

        await using (stream)
        {
            if (stream.Length > 256_000)
            {
                throw new InvalidOperationException("Account sharing settings are too large.");
            }

            var document = await JsonSerializer.DeserializeAsync<SharingDocument>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Account sharing settings are invalid.");
            if (document.SchemaVersion != 1 || document.Settings is null ||
                !string.Equals(document.Settings.AccountId, accountId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Account sharing settings have an unsupported version or identity.");
            }

            var settings = document.Settings.ToSettings();
            settings.Validate();
            return settings;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AccountSharingSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        var path = GetPath(settings.AccountId);
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(_directory);
        }
        else
        {
            Directory.CreateDirectory(_directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var temporaryPath = Path.Combine(_directory, $"{Guid.NewGuid():N}.tmp");
        try
        {
            var normalized = settings with { CalendarIds = settings.CalendarIds?.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() };
            var stored = new StoredSettings(normalized.AccountId, normalized.Enabled, normalized.ShareMail, normalized.ShareCalendars, normalized.CalendarIds);
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(new SharingDocument(1, stored), JsonOptions), cancellationToken);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(temporaryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private string GetPath(string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        return Path.Combine(_directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accountId))) + ".json");
    }

    private sealed record SharingDocument(int SchemaVersion, StoredSettings Settings);

    private sealed record StoredSettings(string AccountId, bool Enabled, bool ShareMail, bool ShareCalendars, IReadOnlyList<string>? CalendarIds)
    {
        public AccountSharingSettings ToSettings() => new(AccountId, Enabled, ShareMail, ShareCalendars, CalendarIds);
    }
}
