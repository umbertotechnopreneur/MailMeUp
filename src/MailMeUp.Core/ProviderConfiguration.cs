namespace MailMeUp.Core;

/// <summary>Stores public provider app identity and an opaque reference to any protected secret.</summary>
public sealed record ProviderConfiguration(string ProviderId, string ClientId, string? ClientSecretReference);

/// <summary>Reports whether a provider app registration is ready for a later account sign-in.</summary>
public sealed record ProviderSetupStatus(
    string ProviderId,
    bool Configured,
    string? ClientIdHint,
    bool ProtectedSecretConfigured);

/// <summary>Reports the result of importing or saving a provider app registration.</summary>
public sealed record ProviderSetupResult(ProviderSetupStatus Status, bool SourceRetained);

/// <summary>Persists non-secret provider app settings independently from account credentials.</summary>
public interface IProviderConfigurationStore
{
    /// <summary>Returns all configured provider app identities.</summary>
    Task<IReadOnlyList<ProviderConfiguration>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns one provider app identity, or null when it is not configured.</summary>
    Task<ProviderConfiguration?> GetAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>Saves public provider app settings and an optional opaque secret reference.</summary>
    Task SaveAsync(ProviderConfiguration configuration, CancellationToken cancellationToken = default);
}

/// <summary>Configures one provider without exposing credentials through MCP.</summary>
public interface IProviderSetupService
{
    /// <summary>Gets the stable provider identifier.</summary>
    string ProviderId { get; }

    /// <summary>Reports whether this provider has the app identity required for sign-in.</summary>
    Task<ProviderSetupStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Imports or saves provider registration input supplied through the local CLI.</summary>
    Task<ProviderSetupResult> ConfigureAsync(string source, CancellationToken cancellationToken = default);
}
