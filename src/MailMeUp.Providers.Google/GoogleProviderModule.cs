using MailMeUp.Core;

namespace MailMeUp.Providers.Google;

/// <summary>Declares the Google provider; runtime capability flags are supplied by registered adapters.</summary>
public sealed class GoogleProviderModule : IProviderModule
{
    /// <inheritdoc />
    public ProviderDescriptor Descriptor { get; } = new("google", "Gmail and Google Calendar", false, false, false);
}
