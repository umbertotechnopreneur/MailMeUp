using System.Reflection;
using System.Text.Json;
using MailMeUp.Application;
using MailMeUp.Core;
using MailMeUp.Mcp;
using MailMeUp.Providers.Google;
using MailMeUp.Providers.Microsoft;
using MailMeUp.Security;
using MailMeUp.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

if (args.Length == 0 || args is ["--help"] or ["-h"])
{
    if (!Console.IsOutputRedirected)
    {
        AnsiConsole.MarkupLine("[bold aquamarine1]MailMeUp[/] · All your inboxes. One conversation.");
    }

    Console.WriteLine("""
        MailMeUp — local read-only email and calendar MCP bridge

        Usage:
          mailmeup --stdio          Run the MCP server on stdin/stdout
          mailmeup status           Report capabilities as JSON
          mailmeup accounts list    List local account metadata as JSON
          mailmeup accounts connect <google|microsoft>
                                    Connect a read-only mail and calendar account
          mailmeup accounts connect <provider> --mail-only
          mailmeup accounts connect <provider> --calendar-only
                                    Request one read-only data category
          mailmeup accounts remove <account-id>
                                    Remove local metadata and credentials
          mailmeup setup status     Report provider app setup as JSON
          mailmeup setup google <client-json>
                                    Import a Google Desktop app client file
          mailmeup setup microsoft <client-id>
                                    Save a Microsoft Application (client) ID
          mailmeup --version        Print the application version
          mailmeup --help           Show this help

        MAILMEUP_DATA_DIR overrides the per-user data directory (absolute path).
        Provider setup and account sign-in are local. Mail and calendar access is exposed only through MCP.
        """);
    return 0;
}

if (args is ["--version"])
{
    Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]);
    return 0;
}

var supportedCommand = args is ["--stdio"] or ["status"] or ["accounts", "list"] or ["setup", "status"] ||
    args is ["setup", "google", _] or ["setup", "microsoft", _] or
    ["accounts", "connect", _] or ["accounts", "connect", _, "--mail-only"] or
    ["accounts", "connect", _, "--calendar-only"] or ["accounts", "remove", _];
if (!supportedCommand)
{
    Console.Error.WriteLine("Unknown command. Run mailmeup --help.");
    return 2;
}

try
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [], DisableDefaults = true });
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

    var dataDirectory = DataDirectory.Resolve(Environment.GetEnvironmentVariable("MAILMEUP_DATA_DIR"));
    builder.Services.AddSingleton<IAccountStore>(_ => new SqliteAccountStore(dataDirectory));
    builder.Services.AddSingleton<IProviderConfigurationStore>(_ => new JsonProviderConfigurationStore(dataDirectory));
    builder.Services.AddSingleton<ISecretStore>(_ => new OsProtectedSecretStore(dataDirectory));
    builder.Services.AddSingleton<IProviderModule, GoogleProviderModule>();
    builder.Services.AddSingleton<IProviderModule, MicrosoftProviderModule>();
    builder.Services.AddSingleton<IProviderSetupService, GoogleProviderSetupService>();
    builder.Services.AddSingleton<IProviderSetupService, MicrosoftProviderSetupService>();
    builder.Services.AddSingleton<IAccountConnector, GoogleAccountConnector>();
    builder.Services.AddSingleton<IAccountConnector, MicrosoftAccountConnector>();
    builder.Services.AddSingleton<IMailReader, GoogleMailReader>();
    builder.Services.AddSingleton<IMailReader, MicrosoftMailReader>();
    builder.Services.AddSingleton<ICalendarReader, GoogleCalendarReader>();
    builder.Services.AddSingleton<ICalendarReader, MicrosoftCalendarReader>();
    builder.Services.AddSingleton<IMailMeUpApplication, MailMeUpApplication>();

    if (args is ["--stdio"])
    {
        builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<MailTools>();
    }

    using var host = builder.Build();
    if (args is ["--stdio"])
    {
        await host.RunAsync();
    }
    else
    {
        var application = host.Services.GetRequiredService<IMailMeUpApplication>();
        object result = args switch
        {
            ["status"] => application.GetStatus(),
            ["accounts", "list"] => new { Accounts = await application.ListAccountsAsync() },
            ["accounts", "connect", var providerId] => await application.ConnectAccountAsync(providerId, new()),
            ["accounts", "connect", var providerId, "--mail-only"] => await application.ConnectAccountAsync(
                providerId,
                new(IncludeMail: true, IncludeCalendar: false)),
            ["accounts", "connect", var providerId, "--calendar-only"] => await application.ConnectAccountAsync(
                providerId,
                new(IncludeMail: false, IncludeCalendar: true)),
            ["accounts", "remove", var accountId] => await application.RemoveAccountAsync(accountId),
            ["setup", "status"] => new { Providers = await application.ListProviderSetupAsync() },
            ["setup", var providerId, var source] => await application.ConfigureProviderAsync(providerId, source),
            _ => throw new InvalidOperationException("The command dispatcher received an unsupported command.")
        };
        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
    }

    return 0;
}
catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or JsonException or SecretStoreException or ProviderAuthenticationException or Microsoft.Data.Sqlite.SqliteException)
{
    // Exception messages may contain user paths or account data; keep diagnostics bounded and private.
    Console.Error.WriteLine($"MailMeUp could not complete the operation ({exception.GetType().Name}). Review the local setup and try again.");
    return 1;
}
