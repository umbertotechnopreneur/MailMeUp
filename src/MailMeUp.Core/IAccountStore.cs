namespace MailMeUp.Core;

/// <summary>Persists local account metadata independently of provider credentials.</summary>
public interface IAccountStore
{
    /// <summary>Lists accounts in stable local identifier order.</summary>
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves metadata for an account after the authentication layer establishes its identity.</summary>
    Task SaveAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>Removes one local account record and returns whether it existed.</summary>
    Task<bool> DeleteAsync(string accountId, CancellationToken cancellationToken = default);
}
