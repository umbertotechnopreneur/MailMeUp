using MailMeUp.Application;
using MailMeUp.Core;
using MailMeUp.Providers.Google;
using MailMeUp.Providers.Microsoft;
using MailMeUp.Security;
using MailMeUp.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace MailMeUp.Cli;

internal static class ServiceRegistration
{
    internal static void AddMailMeUp(this IServiceCollection services, string dataDirectory)
    {
        services.AddSingleton<IAccountStore>(_ => new SqliteAccountStore(dataDirectory));
        services.AddSingleton<IProviderConfigurationStore>(_ => new JsonProviderConfigurationStore(dataDirectory));
        services.AddSingleton<ISecretStore>(_ => new OsProtectedSecretStore(dataDirectory));
        services.AddSingleton<IProviderModule, GoogleProviderModule>();
        services.AddSingleton<IProviderModule, MicrosoftProviderModule>();
        services.AddSingleton<IProviderSetupService, GoogleProviderSetupService>();
        services.AddSingleton<IProviderSetupService, MicrosoftProviderSetupService>();
        services.AddSingleton<IAccountConnector, GoogleAccountConnector>();
        services.AddSingleton<IAccountConnector, MicrosoftAccountConnector>();
        services.AddSingleton<IMailReader, GoogleMailReader>();
        services.AddSingleton<IMailReader, MicrosoftMailReader>();
        services.AddSingleton<ICalendarReader, GoogleCalendarReader>();
        services.AddSingleton<ICalendarReader, MicrosoftCalendarReader>();
        services.AddSingleton<MailMeUpApplication>();
        services.AddSingleton<IMailMeUpApplication>(provider => new LoggingMailMeUpApplication(
            provider.GetRequiredService<MailMeUpApplication>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LoggingMailMeUpApplication>>()));
    }
}
