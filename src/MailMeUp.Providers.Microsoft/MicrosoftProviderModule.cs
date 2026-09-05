using MailMeUp.Core;

namespace MailMeUp.Providers.Microsoft;

/// <summary>Reserves the Microsoft Graph integration boundary and accurately reports its foundation status.</summary>
public sealed class MicrosoftProviderModule : IProviderModule
{
    /// <inheritdoc />
    public ProviderDescriptor Descriptor { get; } = new("microsoft", "Outlook.com and Microsoft 365", false, false);
}
