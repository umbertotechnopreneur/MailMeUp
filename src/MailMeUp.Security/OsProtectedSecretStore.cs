using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Identity.Client.Extensions.Msal;
using ProtectedStorage = Microsoft.Identity.Client.Extensions.Msal.Storage;

namespace MailMeUp.Security;

/// <summary>Protects credential blobs with DPAPI, macOS Keychain, or Linux Secret Service.</summary>
public sealed class OsProtectedSecretStore(string directory) : ISecretStore
{
    private const string KeychainService = "com.mailmeup.credentials";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);
    private readonly string _directory = Path.Combine(Path.GetFullPath(directory), "credentials");
    private readonly string _profileId = Hash(Path.GetFullPath(directory))[..16];

    /// <inheritdoc />
    public async ValueTask<byte[]?> ReadAsync(string reference, CancellationToken cancellationToken = default)
    {
        var key = ValidateAndHash(reference);
        return await ExecuteAsync(key, storage =>
        {
            var data = storage.ReadData();
            return data is null || data.Length == 0 ? null : data;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask WriteAsync(string reference, ReadOnlyMemory<byte> secret, CancellationToken cancellationToken = default)
    {
        if (secret.IsEmpty)
        {
            throw new ArgumentException("Credential material cannot be empty.", nameof(secret));
        }

        var key = ValidateAndHash(reference);
        var copy = secret.ToArray();
        try
        {
            await ExecuteAsync(key, storage =>
            {
                storage.WriteData(copy);
                return true;
            }, cancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copy);
        }
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(string reference, CancellationToken cancellationToken = default)
    {
        var key = ValidateAndHash(reference);
        await ExecuteAsync(key, storage =>
        {
            storage.Clear();
            return true;
        }, cancellationToken);
    }

    private async ValueTask<T> ExecuteAsync<T>(string key, Func<ProtectedStorage, T> operation, CancellationToken cancellationToken)
    {
        var gate = Gates.GetOrAdd($"{_profileId}:{key}", static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            CreatePrivateDirectory(_directory);
            await using var fileLock = await AcquireFileLockAsync(key, cancellationToken);
            var trace = new TraceSource("MailMeUp.ProtectedStorage", SourceLevels.Off);
            try
            {
                trace.Listeners.Clear();
                var storage = ProtectedStorage.Create(CreateProperties(key), trace);
                return operation(storage);
            }
            finally
            {
                trace.Close();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not ArgumentException)
        {
            throw new SecretStoreException("The operating system credential store is unavailable.", exception);
        }
        finally
        {
            gate.Release();
        }
    }

    private StorageCreationProperties CreateProperties(string key) =>
        new StorageCreationPropertiesBuilder($"{key}.bin", _directory)
            .WithMacKeyChain(KeychainService, $"{_profileId}:{key}")
            .WithLinuxKeyring(
                KeychainService,
                "default",
                "MailMeUp protected credentials",
                new KeyValuePair<string, string>("application", "MailMeUp"),
                new KeyValuePair<string, string>("profile", $"{_profileId}:{key}"))
            .Build();

    private async ValueTask<FileStream> AcquireFileLockAsync(string key, CancellationToken cancellationToken)
    {
        var lockDirectory = Path.Combine(_directory, "locks");
        CreatePrivateDirectory(lockDirectory);
        var lockPath = Path.Combine(lockDirectory, $"{key}.lock");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.Asynchronous);
            }
            catch (IOException)
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private static string ValidateAndHash(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (reference.Length > 512)
        {
            throw new ArgumentException("Credential references cannot exceed 512 characters.", nameof(reference));
        }

        return Hash(reference);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static void CreatePrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
