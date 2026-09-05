using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace MailMeUp.Cli;

internal static class CliLogging
{
    internal static Logger Create(CliOptions options) => new LoggerConfiguration()
        .MinimumLevel.Is(options.LogLevel)
        // SDK and transport events may contain request arguments, results or provider exception messages.
        // Emit only our deliberately bounded diagnostics, even at verbose level.
        .Filter.ByIncludingOnly(logEvent =>
            logEvent.Properties.TryGetValue("SourceContext", out var source) &&
            source is ScalarValue { Value: string name } &&
            name.StartsWith("MailMeUp.", StringComparison.Ordinal))
        .WriteTo.Console(
            standardErrorFromLevel: LogEventLevel.Verbose,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}",
            theme: options.Command == CliCommand.Stdio || options.NoColor ||
                Console.IsErrorRedirected || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")) ||
                Environment.GetEnvironmentVariable("TERM") == "dumb"
                    ? ConsoleTheme.None
                    : AnsiConsoleTheme.Code)
        .CreateLogger();
}
