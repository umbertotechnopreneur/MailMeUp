using System.Security.Cryptography;
using System.Text.Json;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using MailMeUp.Security;

namespace MailMeUp.Providers.Google;

internal sealed class ProtectedGoogleTokenStore(ISecretStore secrets, string slot) : IDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _slot = ValidateSlot(slot);
    private readonly string _reference = CreateReference(slot);

    public async Task StoreAsync<T>(string key, T value)
    {
        ValidateOperation<T>(key);
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        try
        {
            await secrets.WriteAsync(_reference, bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public async Task DeleteAsync<T>(string key)
    {
        ValidateOperation<T>(key);
        await secrets.DeleteAsync(_reference);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        ValidateOperation<T>(key);
        var bytes = await secrets.ReadAsync(_reference);
        if (bytes is null)
        {
            return default!;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions)
                ?? throw new SecretStoreException("The protected Google token cache is invalid.");
        }
        catch (JsonException exception)
        {
            throw new SecretStoreException("The protected Google token cache is invalid.", exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public Task ClearAsync() => secrets.DeleteAsync(_reference).AsTask();

    private static string CreateReference(string slot) => $"providers/google/token-cache/{ValidateSlot(slot)}";

    private static string ValidateSlot(string slot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);
        if (slot.Length > 512)
        {
            throw new ArgumentException("Google token slots cannot exceed 512 characters.", nameof(slot));
        }

        return slot;
    }

    private void ValidateOperation<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!string.Equals(key, _slot, StringComparison.Ordinal))
        {
            throw new ArgumentException("The Google token key does not match its protected slot.", nameof(key));
        }

        if (typeof(T) != typeof(TokenResponse))
        {
            throw new NotSupportedException("The Google token store accepts token responses only.");
        }
    }
}
