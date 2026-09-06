using MailMeUp.Desktop.Services;
using MailMeUp.Hosting;
using MailMeUp.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace MailMeUp.Desktop;

/// <summary>Hosts the local Windows setup adapter without starting an MCP process.</summary>
public partial class App : Microsoft.UI.Xaml.Application
{
    private IHost? _host;
    private Window? _window;

    /// <summary>Initializes WinUI resources.</summary>
    public App() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [], DisableDefaults = true });
            // Keep provider diagnostics out of the UI and avoid logging account or credential content.
            builder.Logging.ClearProviders();
            builder.Services.AddMailMeUp(DataDirectory.Resolve(Environment.GetEnvironmentVariable("MAILMEUP_DATA_DIR")));
            builder.Services.AddSingleton<CodexSetupService>();
            builder.Services.AddTransient<MainWindow>();
            _host = builder.Build();
            _window = _host.Services.GetRequiredService<MainWindow>();
        }
        catch (Exception)
        {
            _window = new Window
            {
                Title = "MailMeUp",
                Content = new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "MailMeUp could not open local setup. Check MAILMEUP_DATA_DIR and access to local storage, then reopen the app.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(32)
                }
            };
        }

        _window.Closed += (_, _) => _host?.Dispose();
        _window.Activate();
    }
}
