using System.Runtime.InteropServices;
using MailMeUp.Application;
using MailMeUp.Core;
using MailMeUp.Desktop.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage.Pickers;

namespace MailMeUp.Desktop;

/// <summary>Provides local account, sharing and Codex onboarding through the shared application boundary.</summary>
public sealed partial class MainWindow : Window
{
    private readonly IMailMeUpApplication _application;
    private readonly CodexSetupService _codex;
    private readonly ILogger<MainWindow> _logger;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _operation;
    private bool _loaded;
    private bool _busy;
    private bool _aboutOpen;
    private int _step;
    private bool _sharingDirty;
    private readonly Dictionary<string, TextBlock> _accountSharingLabels = new(StringComparer.Ordinal);

    /// <summary>Creates the centered setup window without starting sign-in or reading mailbox content.</summary>
    public MainWindow(IMailMeUpApplication application, CodexSetupService codex, ILogger<MainWindow> logger)
    {
        _application = application;
        _codex = codex;
        _logger = logger;
        InitializeComponent();
        CenterWindow();
        Root.Loaded += Root_Loaded;
        Closed += (_, _) =>
        {
            _lifetime.Cancel();
            _operation?.Cancel();
        };
    }

    private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var scale = Math.Max(1.0, GetDpiForWindow(handle) / 96.0);
        var width = Math.Min((int)(1000 * scale), area.Width);
        var height = Math.Min((int)(780 * scale), area.Height);
        AppWindow.MoveAndResize(new RectInt32(
            area.X + (area.Width - width) / 2,
            area.Y + (area.Height - height) / 2,
            width,
            height));
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint GetDpiForWindow(IntPtr window);

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        Steps.SelectedIndex = 0;
        ShowStep(0);
        await RunAsync("Loading local setup…", RefreshAccountsAsync);
    }

    private void ShowStep(int step)
    {
        _step = step;
        WelcomePage.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
        AccountsPage.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        SharingPage.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        CodexPage.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        BackButton.IsEnabled = step > 0 && !_busy;
        NextButton.Content = step switch { 0 => "Get started", 1 => "Choose sharing", 2 => "Connect to Codex", _ => "Close setup" };
        if (step == 3)
        {
            CodexCommands.Text = _codex.GetPreview().ManualCommands;
        }
    }

    private void Steps_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || Steps.SelectedIndex < 0) return;
        if (_sharingDirty && Steps.SelectedIndex != 2)
        {
            Steps.SelectedIndex = 2;
            SetNotice("Unsaved sharing choices", "Save the changed account choices before leaving this step.", InfoBarSeverity.Warning);
            return;
        }
        ShowStep(Steps.SelectedIndex);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => Steps.SelectedIndex = Math.Max(0, _step - 1);

    private async void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _aboutOpen) return;
        _aboutOpen = true;
        try
        {
            var dialog = new AboutDialog { XamlRoot = Root.XamlRoot };
            using var cancellation = _lifetime.Token.Register(() => DispatcherQueue.TryEnqueue(() => dialog.Hide()));
            await dialog.ShowAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning("About dialog could not open ({ErrorType})", exception.GetType().Name);
            if (!_lifetime.IsCancellationRequested)
                SetNotice("Support", "Visit github.com/umbertotechnopreneur/MailMeUp for support or umbertogiacobbi.biz for the creator's website.", InfoBarSeverity.Informational);
        }
        finally
        {
            _aboutOpen = false;
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_step == 3) Close();
        else Steps.SelectedIndex = _step + 1;
    }

    private async Task RefreshAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = await _application.ListAccountsAsync(cancellationToken);
        var providers = await _application.ListProviderSetupAsync(cancellationToken);
        var settings = (await _application.ListAccountSharingAsync(cancellationToken)).ToDictionary(item => item.AccountId, StringComparer.Ordinal);
        ProviderStatusText.Text = string.Join("  ·  ", new[] { "google", "microsoft" }.Select(id =>
            $"{(id == "google" ? "Google" : "Microsoft")}: {(providers.Any(provider => provider.ProviderId == id && provider.Configured) ? "ready to sign in" : "app registration needed")}"));
        ConnectedAccounts.Children.Clear();
        _accountSharingLabels.Clear();
        SharingAccounts.Children.Clear();
        _sharingDirty = false;
        if (accounts.Count == 0)
        {
            ConnectedAccounts.Children.Add(Body("No accounts connected yet. Add your first account above."));
            SharingAccounts.Children.Add(Body("Connect an account in step 1 to choose what to share."));
            return;
        }

        foreach (var account in accounts)
        {
            var sharing = settings.GetValueOrDefault(account.Id) ?? new AccountSharingSettings(account.Id);
            var details = new StackPanel { Spacing = 5 };
            details.Children.Add(new TextBlock { Text = account.EmailAddress, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            var sharingLabel = Body(SharingLabel(account, sharing));
            _accountSharingLabels[account.Id] = sharingLabel;
            details.Children.Add(sharingLabel);
            ConnectedAccounts.Children.Add(Card(details));
            SharingAccounts.Children.Add(CreateSharingCard(account, sharing));
        }
    }

    private Border CreateSharingCard(Account account, AccountSharingSettings initial)
    {
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = account.EmailAddress, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        var enabled = new CheckBox { Content = "Share this account with my assistant", IsChecked = initial.Enabled };
        var mail = new CheckBox { Content = account.MailReadEnabled ? "Mail" : "Mail (not granted at sign-in)", IsChecked = initial.ShareMail && account.MailReadEnabled };
        var calendars = new CheckBox { Content = account.CalendarReadEnabled ? "Calendars" : "Calendars (not granted at sign-in)", IsChecked = initial.ShareCalendars && account.CalendarReadEnabled };
        var allCalendars = new CheckBox { Content = "All current and future calendars", IsChecked = initial.CalendarIds is null };
        var calendarList = new StackPanel { Spacing = 6, Margin = new Thickness(20, 0, 0, 0) };
        var load = new Button { Content = "Choose individual calendars" };
        var summary = Body(initial.CalendarIds is null ? "All calendars selected." : $"{initial.CalendarIds.Count} individual calendars selected.");
        var save = new Button { Content = "Save choices", IsEnabled = false };
        var saved = Body("Choices saved on this device.");
        var selectedIds = initial.CalendarIds?.ToHashSet(StringComparer.Ordinal) ?? [];
        var calendarChecks = new List<(string Id, CheckBox Control)>();
        var dirty = false;

        void MarkDirty()
        {
            dirty = true;
            save.IsEnabled = true;
            saved.Text = "Unsaved changes";
            save.Tag = true;
            _sharingDirty = true;
        }

        void UpdateControls()
        {
            mail.IsEnabled = enabled.IsChecked == true && account.MailReadEnabled;
            calendars.IsEnabled = enabled.IsChecked == true && account.CalendarReadEnabled;
            var canChoose = enabled.IsChecked == true && calendars.IsChecked == true && account.CalendarReadEnabled;
            allCalendars.IsEnabled = canChoose;
            load.IsEnabled = canChoose;
            foreach (var item in calendarChecks) item.Control.IsEnabled = canChoose && allCalendars.IsChecked != true;
        }

        void Changed(object sender, RoutedEventArgs e)
        {
            UpdateControls();
            MarkDirty();
        }

        foreach (var check in new[] { enabled, mail, calendars, allCalendars })
        {
            check.Checked += Changed;
            check.Unchecked += Changed;
        }

        load.Click += async (_, _) => await RunAsync("Loading calendar names…", async cancellationToken =>
        {
            if (calendarChecks.Count > 0)
                selectedIds = calendarChecks.Where(item => item.Control.IsChecked == true).Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
            var available = await _application.ListAvailableCalendarsAsync(account.Id, cancellationToken);
            calendarList.Children.Clear();
            calendarChecks.Clear();
            foreach (var calendar in available)
            {
                var check = new CheckBox
                {
                    Content = calendar.Name + (calendar.Primary ? " (primary)" : ""),
                    IsChecked = allCalendars.IsChecked == true || selectedIds.Contains(calendar.ProviderCalendarId)
                };
                check.Checked += (_, _) => MarkDirty();
                check.Unchecked += (_, _) => MarkDirty();
                calendarChecks.Add((calendar.ProviderCalendarId, check));
                calendarList.Children.Add(check);
            }
            // Preserve inaccessible saved IDs when discovery returns no rows; never silently widen sharing.
            summary.Text = available.Count == 0 ? "No calendars returned. Saved selections are kept." : "Choose the calendars your assistant may read.";
            if (available.Count > 0) allCalendars.IsChecked = false;
            UpdateControls();
        });

        save.Click += async (_, _) => await RunAsync("Saving sharing choices…", async cancellationToken =>
        {
            var ids = allCalendars.IsChecked == true
                ? null
                : calendarChecks.Count > 0
                    ? calendarChecks.Where(item => item.Control.IsChecked == true).Select(item => item.Id).ToArray()
                    : selectedIds.ToArray();
            var result = await _application.SaveAccountSharingAsync(new AccountSharingSettings(
                account.Id,
                Enabled: enabled.IsChecked == true,
                ShareMail: mail.IsChecked == true && account.MailReadEnabled,
                ShareCalendars: calendars.IsChecked == true && account.CalendarReadEnabled,
                CalendarIds: ids), cancellationToken);
            selectedIds = result.CalendarIds?.ToHashSet(StringComparer.Ordinal) ?? [];
            dirty = false;
            save.Tag = false;
            save.IsEnabled = false;
            saved.Text = "Saved. Applies to future assistant reads.";
            if (_accountSharingLabels.TryGetValue(account.Id, out var sharingLabel)) sharingLabel.Text = SharingLabel(account, result);
            summary.Text = result.CalendarIds is null ? "All calendars selected." : $"{result.CalendarIds.Count} individual calendars selected.";
            _sharingDirty = SharingAccounts.Children.OfType<Border>().Any(card =>
                card.Child is StackPanel stack && stack.Children.OfType<Button>().Any(button => button.Tag is true));
            SetNotice("Sharing choices saved", "Information already returned to an assistant stays in its conversation.", InfoBarSeverity.Success);
        });

        // Dirty state is kept per card so saving one account does not discard another account's choices.
        save.Loaded += (_, _) => save.IsEnabled = dirty;
        panel.Children.Add(enabled);
        var categories = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
        categories.Children.Add(mail);
        categories.Children.Add(calendars);
        panel.Children.Add(categories);
        panel.Children.Add(allCalendars);
        panel.Children.Add(summary);
        panel.Children.Add(load);
        panel.Children.Add(calendarList);
        panel.Children.Add(save);
        panel.Children.Add(saved);
        UpdateControls();
        return Card(panel);
    }

    private async void GoogleButton_Click(object sender, RoutedEventArgs e) => await ConnectAsync("google");

    private async void MicrosoftButton_Click(object sender, RoutedEventArgs e) => await ConnectAsync("microsoft");

    private async Task ConnectAsync(string provider)
    {
        if (RequestMail.IsChecked != true && RequestCalendars.IsChecked != true)
        {
            SetNotice("Choose read access", "Select mail, calendars, or both before signing in.", InfoBarSeverity.Warning);
            return;
        }

        await RunAsync($"Connecting {ProviderName(provider)} — finish sign-in in your browser…", async cancellationToken =>
        {
            var setup = await _application.ListProviderSetupAsync(cancellationToken);
            if (!setup.Any(item => item.ProviderId == provider && item.Configured))
            {
                var configured = provider == "google"
                    ? await ConfigureGoogleAsync(cancellationToken)
                    : await ConfigureMicrosoftAsync(cancellationToken);
                if (!configured) return;
            }

            await _application.ConnectAccountAsync(provider,
                new AccountConnectionOptions(RequestMail.IsChecked == true, RequestCalendars.IsChecked == true, ShareWithAssistant: false), cancellationToken);
            await RefreshAccountsAsync(cancellationToken);
            SetNotice("Account connected", "New accounts start with sharing off. Add another account or continue to choose sharing. Reconnected accounts keep their saved choices.", InfoBarSeverity.Success);
        }, TimeSpan.FromMinutes(5));
    }

    private async Task<bool> ConfigureGoogleAsync(CancellationToken cancellationToken)
    {
        ActivityText.Text = "Select your Google Desktop OAuth client JSON…";
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Downloads, ViewMode = PickerViewMode.List };
        picker.FileTypeFilter.Add(".json");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (file is null) return false;
        await _application.ConfigureProviderAsync("google", file.Path, cancellationToken);
        SetNotice("Google app configured", "The original JSON file is still in its original folder. Keep it private.", InfoBarSeverity.Informational);
        ActivityText.Text = "Connecting Google — finish sign-in in your browser…";
        return true;
    }

    private async Task<bool> ConfigureMicrosoftAsync(CancellationToken cancellationToken)
    {
        var input = new TextBox { Header = "Application (client) ID", PlaceholderText = "00000000-0000-0000-0000-000000000000" };
        var content = new StackPanel { Spacing = 14 };
        content.Children.Add(Body("Enter the public client ID of your Microsoft desktop app registration. A client secret is not needed."));
        content.Children.Add(input);
        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot,
            Title = "Set up Microsoft sign-in",
            Content = content,
            PrimaryButtonText = "Save and sign in",
            CloseButtonText = "Cancel",
            IsPrimaryButtonEnabled = false,
            DefaultButton = ContentDialogButton.Primary
        };
        input.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = Guid.TryParseExact(input.Text.Trim(), "D", out _);
        using var registration = cancellationToken.Register(() => DispatcherQueue.TryEnqueue(() => dialog.Hide()));
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return false;
        cancellationToken.ThrowIfCancellationRequested();
        await _application.ConfigureProviderAsync("microsoft", input.Text.Trim(), cancellationToken);
        return true;
    }

    private async void RefreshCodexButton_Click(object sender, RoutedEventArgs e) =>
        await RunAsync("Reading local Codex configuration…", RefreshCodexAsync);

    private async Task RefreshCodexAsync(CancellationToken cancellationToken)
    {
        var status = await _codex.GetStatusAsync(cancellationToken);
        CodexStatusText.Text = status.Message;
        InstallPluginButton.IsEnabled = status.CanInstall;
        CodexCommands.Text = _codex.GetPreview().ManualCommands;
    }

    private async void InstallPluginButton_Click(object sender, RoutedEventArgs e) =>
        await RunAsync("Installing the local Codex plugin…", async cancellationToken =>
        {
            var result = await _codex.InstallPluginAsync(cancellationToken);
            CodexStatusText.Text = result.Status.Message;
            InstallPluginButton.IsEnabled = result.Status.CanInstall;
            SetNotice(result.Success ? "Plugin configured" : "Setup needs attention", result.Message,
                result.Success ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        }, TimeSpan.FromMinutes(2));

    private async void PreparePluginButton_Click(object sender, RoutedEventArgs e) =>
        await RunAsync("Preparing local plugin files…", async cancellationToken =>
        {
            var preview = await _codex.PreparePluginAsync(cancellationToken);
            CodexCommands.Text = preview.ManualCommands;
            SetNotice("Plugin files prepared", "Copy the commands and run them where the Codex CLI is available. This step alone does not install the plugin.", InfoBarSeverity.Informational);
        });

    private void CopyCommandsButton_Click(object sender, RoutedEventArgs e)
    {
        var data = new DataPackage();
        data.SetText(CodexCommands.Text);
        try
        {
            Clipboard.SetContent(data);
            SetNotice("Copied", "The setup commands are on your clipboard.", InfoBarSeverity.Informational);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Clipboard operation failed ({ErrorType})", exception.GetType().Name);
            SetNotice("Clipboard unavailable", "Select and copy the commands from the text box.", InfoBarSeverity.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _operation?.Cancel();

    private async Task RunAsync(string activity, Func<CancellationToken, Task> action, TimeSpan? timeout = null)
    {
        if (_busy || _lifetime.IsCancellationRequested) return;
        _busy = true;
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _operation = operation;
        operation.CancelAfter(timeout ?? TimeSpan.FromSeconds(60));
        PageHost.IsEnabled = false;
        Steps.IsEnabled = false;
        NextButton.IsEnabled = false;
        BackButton.IsEnabled = false;
        AboutButton.IsEnabled = false;
        ActivityText.Text = activity;
        Activity.Visibility = Visibility.Visible;
        Notice.IsOpen = false;
        try
        {
            await action(operation.Token);
        }
        catch (OperationCanceledException)
        {
            if (!_lifetime.IsCancellationRequested)
                SetNotice("Operation stopped", "The operation was cancelled or timed out. Any completed setup steps remain saved; you can retry.", InfoBarSeverity.Warning);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Desktop setup operation failed ({ErrorType})", exception.GetType().Name);
            if (!_lifetime.IsCancellationRequested)
                SetNotice("Could not complete setup", "Check the provider app registration, network and local storage access, then retry. Existing credentials and saved sharing choices are kept.", InfoBarSeverity.Error);
        }
        finally
        {
            _operation = null;
            _busy = false;
            if (!_lifetime.IsCancellationRequested)
            {
                PageHost.IsEnabled = true;
                Steps.IsEnabled = true;
                NextButton.IsEnabled = true;
                BackButton.IsEnabled = _step > 0;
                AboutButton.IsEnabled = true;
                Activity.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void SetNotice(string title, string message, InfoBarSeverity severity)
    {
        Notice.Title = title;
        Notice.Message = message;
        Notice.Severity = severity;
        Notice.IsOpen = true;
    }

    private TextBlock Body(string text) => new() { Text = text, Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["BodyText"] };

    private Border Card(UIElement child) => new() { Child = child, Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["SetupCard"] };

    private static string ProviderName(string provider) => provider == "google" ? "Google" : provider == "microsoft" ? "Microsoft" : provider;

    private static string SharingLabel(Account account, AccountSharingSettings settings) =>
        $"{ProviderName(account.Provider)} · {(settings.Enabled && (settings.ShareMail && account.MailReadEnabled || settings.ShareCalendars && account.CalendarReadEnabled && settings.CalendarIds is not { Count: 0 }) ? "Sharing enabled" : "Sharing off")}";
}
