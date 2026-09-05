using MailMeUp.Core;

namespace MailMeUp.Providers.Microsoft;

/// <summary>Declares the Microsoft provider; runtime capability flags are supplied by registered adapters.</summary>
public sealed class MicrosoftProviderModule : IProviderModule
{
    /// <inheritdoc />
    public ProviderDescriptor Descriptor { get; } = new("microsoft", "Microsoft mail and calendars", false, false, false);
}
