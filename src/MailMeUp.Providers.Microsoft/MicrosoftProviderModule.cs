using MailMeUp.Core;

namespace MailMeUp.Providers.Microsoft;

/// <summary>Reserves the Microsoft Graph integration boundary and accurately reports its foundation status.</summary>
public sealed class MicrosoftProviderModule : IProviderModule
{
    /// <inheritdoc />
    public ProviderDescriptor Descriptor { get; } = new("microsoft", "Microsoft mail and calendars", false, false, false);
}
