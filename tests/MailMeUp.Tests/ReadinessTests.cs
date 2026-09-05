using MailMeUp.Application;
using MailMeUp.Core;
using MailMeUp.Providers.Google;
using MailMeUp.Providers.Microsoft;
using MailMeUp.Storage;
using Xunit;

namespace MailMeUp.Tests;

public sealed class ReadinessTests
{
    [Fact]
    public void FoundationNeverAdvertisesUnimplementedMailCapabilities()
    {
        var application = new MailMeUpApplication(new SqliteAccountStore(Path.GetTempPath()),
            new IProviderModule[] { new GoogleProviderModule(), new MicrosoftProviderModule() });

        var status = application.GetStatus();
        Assert.Equal("foundation", status.Stage);
        Assert.False(status.CanConnectAccounts);
        Assert.Equal(2, status.Providers.Count);
        Assert.All(status.Providers, provider =>
        {
            Assert.False(provider.AuthenticationAvailable);
            Assert.False(provider.MailReadAvailable);
        });
    }
}
