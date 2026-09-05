namespace MailMeUp.Core;

/// <summary>Reports a provider's actual implementation readiness.</summary>
public sealed record ProviderDescriptor(string Id, string DisplayName, bool AuthenticationAvailable, bool MailReadAvailable);

/// <summary>Exposes provider readiness without pretending that mail operations are implemented.</summary>
public interface IProviderModule
{
    /// <summary>Gets the module's public capabilities.</summary>
    ProviderDescriptor Descriptor { get; }
}
