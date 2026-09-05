using MailMeUp.Core;

namespace MailMeUp.Application;

/// <summary>Coordinates local metadata and reports explicitly unfinished provider integrations.</summary>
public sealed class MailMeUpApplication(IAccountStore accounts, IEnumerable<IProviderModule> providers) : IMailMeUpApplication
{
    private readonly IReadOnlyList<ProviderDescriptor> _providers = providers
        .Select(provider => provider.Descriptor).OrderBy(provider => provider.Id, StringComparer.Ordinal).ToArray();

    /// <inheritdoc />
    public ApplicationStatus GetStatus() => new("foundation", "stdio", true, _providers.Any(provider => provider.AuthenticationAvailable), _providers);

    /// <inheritdoc />
    public Task<IReadOnlyList<Account>> ListAccountsAsync(CancellationToken cancellationToken = default) => accounts.ListAsync(cancellationToken);
}
