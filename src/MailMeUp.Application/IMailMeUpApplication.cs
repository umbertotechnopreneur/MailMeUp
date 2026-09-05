using MailMeUp.Core;

namespace MailMeUp.Application;

/// <summary>The application boundary shared by command-line and MCP adapters.</summary>
public interface IMailMeUpApplication
{
    /// <summary>Reports this build's readiness and supported provider modules.</summary>
    ApplicationStatus GetStatus();

    /// <summary>Returns non-secret account metadata from the local registry.</summary>
    Task<IReadOnlyList<Account>> ListAccountsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Describes capabilities available in the running build.</summary>
public sealed record ApplicationStatus(string Stage, string Transport, bool ReadOnly, bool CanConnectAccounts, IReadOnlyList<ProviderDescriptor> Providers);
