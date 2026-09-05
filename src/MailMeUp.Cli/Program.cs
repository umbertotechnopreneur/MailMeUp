using System.Reflection;
using System.Text.Json;
using MailMeUp.Application;
using MailMeUp.Core;
using MailMeUp.Mcp;
using MailMeUp.Providers.Google;
using MailMeUp.Providers.Microsoft;
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
        MailMeUp — local email MCP bridge (foundation)

        Usage:
          mailmeup --stdio          Run the MCP server on stdin/stdout
          mailmeup status           Report capabilities as JSON
          mailmeup accounts list    List local account metadata as JSON
          mailmeup --version        Print the application version
          mailmeup --help           Show this help

        MAILMEUP_DATA_DIR overrides the per-user data directory (absolute path).
        Account authentication and mail retrieval are not implemented yet.
        """);
    return 0;
}

if (args is ["--version"])
{
    Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]);
    return 0;
}

if (args is not (["--stdio"] or ["status"] or ["accounts", "list"]))
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

    builder.Services.AddSingleton<IAccountStore>(_ => new SqliteAccountStore(DataDirectory.Resolve(Environment.GetEnvironmentVariable("MAILMEUP_DATA_DIR"))));
    builder.Services.AddSingleton<IProviderModule, GoogleProviderModule>();
    builder.Services.AddSingleton<IProviderModule, MicrosoftProviderModule>();
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
        object result = args is ["status"]
            ? application.GetStatus()
            : new { Accounts = await application.ListAccountsAsync() };
        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
    }

    return 0;
}
catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
{
    // Exception messages may contain user paths or account data; keep diagnostics bounded and private.
    Console.Error.WriteLine($"MailMeUp could not complete the operation ({exception.GetType().Name}). Check the data directory and database version.");
    return 1;
}
