namespace MailMeUp.Security;

/// <summary>Future OS-protected credential persistence boundary. No implementation ships in the foundation.</summary>
public interface ISecretStore
{
    /// <summary>Reads a protected credential blob by its opaque reference; returns null when absent.</summary>
    ValueTask<byte[]?> ReadAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Stores a credential blob using OS-backed protection, or fails if protection is unavailable.</summary>
    ValueTask WriteAsync(string reference, ReadOnlyMemory<byte> secret, CancellationToken cancellationToken = default);

    /// <summary>Deletes a stored credential blob without exposing its contents.</summary>
    ValueTask DeleteAsync(string reference, CancellationToken cancellationToken = default);
}
