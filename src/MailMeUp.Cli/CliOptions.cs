using Serilog.Events;

namespace MailMeUp.Cli;

internal enum CliCommand
{
    Help,
    Version,
    Stdio,
    Status,
    Accounts,
    Connect,
    Remove,
    SetupStatus,
    Setup
}

internal sealed record CliOptions(
    CliCommand Command,
    string? Provider = null,
    string? Value = null,
    bool IncludeMail = true,
    bool IncludeCalendar = true,
    bool Json = false,
    bool NoColor = false,
    bool NoAnimation = false,
    LogEventLevel LogLevel = LogEventLevel.Warning)
{
    internal static CliOptions Parse(string[] args)
    {
        var operands = new List<string>();
        var flags = new HashSet<string>(StringComparer.Ordinal);
        string? level = null;
        var literal = false;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!literal && argument == "--")
            {
                literal = true;
                continue;
            }

            if (!literal && argument is "--json" or "--no-color" or "--no-animation" or "--log-level" or "--mail-only" or "--calendar-only")
            {
                if (!flags.Add(argument))
                {
                    throw new CliUsageException("Specify each option only once. Run mailmeup --help for usage.");
                }

                if (argument == "--log-level")
                {
                    if (++index == args.Length)
                    {
                        throw new CliUsageException("--log-level requires verbose, debug, information, warning, error or fatal.");
                    }

                    level = args[index];
                }
            }
            else
            {
                if (!literal && argument.StartsWith('-') && argument is not ("--help" or "-h" or "--version" or "--stdio"))
                {
                    throw new CliUsageException("Unknown option. Run mailmeup --help. Use -- before a value that starts with a hyphen.");
                }

                operands.Add(argument);
            }
        }

        var options = operands.ToArray() switch
        {
            [] or ["--help"] or ["-h"] => new CliOptions(CliCommand.Help),
            ["--version"] => new CliOptions(CliCommand.Version),
            ["--stdio"] => new CliOptions(CliCommand.Stdio),
            ["status"] => new CliOptions(CliCommand.Status),
            ["accounts", "list"] => new CliOptions(CliCommand.Accounts),
            ["accounts", "connect", var provider] when provider is "google" or "microsoft" => new CliOptions(CliCommand.Connect, Provider: provider),
            ["accounts", "remove", var id] when !string.IsNullOrWhiteSpace(id) => new CliOptions(CliCommand.Remove, Value: id),
            ["setup", "status"] => new CliOptions(CliCommand.SetupStatus),
            ["setup", var provider, var source] when (provider is "google" or "microsoft") && !string.IsNullOrWhiteSpace(source) =>
                new CliOptions(CliCommand.Setup, Provider: provider, Value: source),
            ["accounts", "--help"] or ["setup", "--help"] => new CliOptions(CliCommand.Help),
            _ => throw new CliUsageException("Unknown or incomplete command. Run mailmeup --help. Providers: google, microsoft.")
        };

        var mailOnly = flags.Contains("--mail-only");
        var calendarOnly = flags.Contains("--calendar-only");
        if ((mailOnly || calendarOnly) && (options.Command != CliCommand.Connect || mailOnly && calendarOnly))
        {
            throw new CliUsageException("Use either --mail-only or --calendar-only with accounts connect, or omit both to request both categories.");
        }

        if (flags.Contains("--json") && options.Command is CliCommand.Help or CliCommand.Version or CliCommand.Stdio)
        {
            throw new CliUsageException("--json applies to status, accounts and setup commands. MCP already uses JSON-RPC.");
        }

        return options with
        {
            IncludeMail = !calendarOnly,
            IncludeCalendar = !mailOnly,
            Json = flags.Contains("--json"),
            NoColor = flags.Contains("--no-color"),
            NoAnimation = flags.Contains("--no-animation"),
            LogLevel = ParseLogLevel(level ?? Environment.GetEnvironmentVariable("MAILMEUP_LOG_LEVEL"))
        };
    }

    private static LogEventLevel ParseLogLevel(string? value) => value?.ToLowerInvariant() switch
    {
        null or "" or "warning" => LogEventLevel.Warning,
        "verbose" => LogEventLevel.Verbose,
        "debug" => LogEventLevel.Debug,
        "information" => LogEventLevel.Information,
        "error" => LogEventLevel.Error,
        "fatal" => LogEventLevel.Fatal,
        _ => throw new CliUsageException("Invalid log level. Use verbose, debug, information, warning, error or fatal (--log-level or MAILMEUP_LOG_LEVEL).")
    };
}

internal sealed class CliUsageException(string message) : Exception(message)
{
}
