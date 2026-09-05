namespace MailMeUp.Security;

/// <summary>OS-protected credential persistence boundary.</summary>
public interface ISecretStore
{
    /// <summary>Reads a protected credential blob by its opaque reference; returns null when absent.</summary>
    ValueTask<byte[]?> ReadAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Stores a credential blob using OS-backed protection, or fails if protection is unavailable.</summary>
    ValueTask WriteAsync(string reference, ReadOnlyMemory<byte> secret, CancellationToken cancellationToken = default);

    /// <summary>Deletes a stored credential blob without exposing its contents.</summary>
    ValueTask DeleteAsync(string reference, CancellationToken cancellationToken = default);
}

/// <summary>Indicates that the operating system could not safely protect credential material.</summary>
public sealed class SecretStoreException : Exception
{
    /// <summary>Creates a sanitized credential-store failure.</summary>
    public SecretStoreException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a sanitized credential-store failure.</summary>
    public SecretStoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
