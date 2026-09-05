using MailMeUp.Application;
using Microsoft.Extensions.Logging;

namespace MailMeUp.Cli;

internal sealed class CliRunner(IMailMeUpApplication application, CliPresentation presentation, ILogger<CliRunner> logger)
{
    internal async Task RunAsync(CliOptions options, CancellationToken cancellationToken)
    {
        presentation.WriteBanner();
        logger.LogDebug("Executing command {Command}", options.Command);
        var activity = options.Command switch
        {
            CliCommand.Connect => "Complete sign-in in your browser. Press Ctrl+C to cancel.",
            CliCommand.Setup => "Saving provider setup...",
            CliCommand.Remove => "Removing the local account...",
            _ => "Reading local status..."
        };
        var result = await presentation.RunWithStatusAsync(activity, async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return options.Command switch
            {
                CliCommand.Status => (object)application.GetStatus(),
                CliCommand.Accounts => new AccountListOutput(await application.ListAccountsAsync(cancellationToken)),
                CliCommand.Connect => await application.ConnectAccountAsync(options.Provider!,
                    new(options.IncludeMail, options.IncludeCalendar), cancellationToken),
                CliCommand.Remove => await application.RemoveAccountAsync(options.Value!, cancellationToken),
                CliCommand.SetupStatus => new ProviderListOutput(await application.ListProviderSetupAsync(cancellationToken)),
                CliCommand.Setup => await application.ConfigureProviderAsync(options.Provider!, options.Value!, cancellationToken),
                _ => throw new InvalidOperationException("The command dispatcher received an unsupported command.")
            };
        });
        presentation.WriteResult(result);
        logger.LogDebug("Command {Command} completed", options.Command);
    }
}
