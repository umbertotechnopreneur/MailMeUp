using MailMeUp.Core;

namespace MailMeUp.Providers.Google;

/// <summary>Reserves the Google integration boundary and accurately reports its foundation status.</summary>
public sealed class GoogleProviderModule : IProviderModule
{
    /// <inheritdoc />
    public ProviderDescriptor Descriptor { get; } = new("google", "Gmail and Google Calendar", false, false, false);
}
