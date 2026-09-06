using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MailMeUp.Storage;
using Microsoft.Extensions.Logging;

namespace MailMeUp.Desktop.Services;

/// <summary>Prepares and installs the local Codex plugin only after an explicit desktop action.</summary>
public sealed class CodexSetupService(ILogger<CodexSetupService> logger) : IDisposable
{
    private const string PluginId = "mailmeup@mailmeup-local";
    private const string MarketplaceName = "mailmeup-local";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim installationLock = new(1, 1);

    /// <summary>Releases the local installation synchronization primitive.</summary>
    public void Dispose() => installationLock.Dispose();

    /// <summary>Returns reviewable commands and stable paths without creating files or starting Codex.</summary>
    public CodexSetupPreview GetPreview()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            throw new InvalidOperationException("The Windows local application directory is unavailable.");
        }

        var alias = Path.Combine(local, "Microsoft", "WindowsApps", "mailmeup.exe");
        var pluginDirectory = Path.Combine(ResolveDataDirectory(), "codex-plugin");
        var commands = $"codex plugin marketplace add {QuotePowerShell(pluginDirectory)}{Environment.NewLine}"
            + $"codex plugin add {PluginId}{Environment.NewLine}codex plugin list --json";
        var mcpCommand = $"codex mcp add mailmeup --env {QuotePowerShell($"MAILMEUP_DATA_DIR={ResolveDataDirectory()}")} "
            + $"-- {QuotePowerShell(alias)} --stdio";
        return new(alias, pluginDirectory, commands, mcpCommand);
    }

    /// <summary>Copies only the bundled MailMeUp marketplace into its writable local directory.</summary>
    public async Task<CodexSetupPreview> PreparePluginAsync(CancellationToken cancellationToken = default)
    {
        await installationLock.WaitAsync(cancellationToken);
        try
        {
            return await PreparePluginCoreAsync(cancellationToken);
        }
        finally
        {
            installationLock.Release();
        }
    }

    /// <summary>Reads Codex's installed-plugin and MCP configuration without starting the MailMeUp server.</summary>
    public async Task<CodexSetupStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var preview = GetPreview();
            if (!File.Exists(preview.StableExecutablePath))
            {
                return State("AliasUnavailable", "Install the MailMeUp MSIX and enable its mailmeup.exe app execution alias in Windows Settings.");
            }

            var executable = FindCodexExecutable();
            if (executable is null)
            {
                return State("CodexUnavailable", "The native Codex CLI was not found. Prepare the plugin files, then run the displayed commands in your Codex terminal. A script-only CLI installation requires this manual route.");
            }

            var mcp = await RunCodexAsync(executable, ["mcp", "list", "--json"], cancellationToken);
            if (!mcp.Success || !TryReadDirectRegistration(mcp.Output, out var hasDirectRegistration))
            {
                return State("ConfigurationUnknown", "Codex MCP configuration could not be read. Check it in Codex before adding another MailMeUp connection.");
            }

            var plugins = await RunCodexAsync(executable, ["plugin", "list", "--json"], cancellationToken);
            if (!plugins.Success || !TryReadPluginState(plugins.Output, out var installed, out var enabled, out var otherPlugin))
            {
                return new("PluginStatusUnknown", "This Codex CLI did not return a supported plugin status. Update Codex or use its plugin settings; no installation was attempted.", false, false, hasDirectRegistration);
            }

            if (hasDirectRegistration)
            {
                return new("DirectRegistrationExists", "A direct MailMeUp MCP connection already exists. Review and remove that connection in Codex before installing this plugin to avoid duplicate tools. MailMeUp will not remove it automatically.", false, installed && enabled, true);
            }

            if (otherPlugin)
            {
                return State("OtherPluginExists", "MailMeUp is already installed from another marketplace. Manage that plugin in Codex before adding this local copy.");
            }

            var marketplaces = await RunCodexAsync(executable, ["plugin", "marketplace", "list", "--json"], cancellationToken);
            if (!marketplaces.Success || !TryReadMarketplaceState(marketplaces.Output, preview.PluginDirectory, out var collision))
            {
                return State("MarketplaceStatusUnknown", "The Codex marketplace configuration could not be read. Review the MailMeUp local marketplace in Codex.");
            }

            if (collision)
            {
                return State("MarketplaceConflict", "A different source already uses the mailmeup-local marketplace name. Resolve it in Codex before installing this copy.");
            }

            if (installed)
            {
                return enabled
                    ? new("PluginConfigured", "Codex reports the MailMeUp plugin installed and enabled. Start a new Codex task to load its tools. This checks configuration only; the server connection has not been tested.", true, true, false)
                    : State("PluginDisabled", "The MailMeUp plugin is installed but disabled. Enable it in Codex, then refresh the status.");
            }

            return new("ReadyToInstall", "The Windows alias and Codex CLI are available. Install the local plugin to connect your selected accounts.", true, false, false);
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
            logger.LogWarning("Codex configuration inspection failed with {FailureType}.", exception.GetType().Name);
            return State("ConfigurationUnknown", "Codex configuration could not be read. Check that Codex is installed and that its local configuration is accessible, then refresh.");
        }
    }

    /// <summary>Installs the app-owned marketplace and plugin, preserving all direct MCP registrations.</summary>
    public async Task<CodexSetupResult> InstallPluginAsync(CancellationToken cancellationToken = default)
    {
        await installationLock.WaitAsync(cancellationToken);
        try
        {
            var status = await GetStatusAsync(cancellationToken);
            if (!status.CanInstall)
            {
                return new(false, status.Message, status);
            }

            var executable = FindCodexExecutable();
            if (executable is null)
            {
                return new(false, "The Codex executable is no longer available. Refresh the setup status.", State("CodexUnavailable", "Codex CLI is unavailable."));
            }

            var preview = await PreparePluginCoreAsync(cancellationToken);
            var marketplace = await RunCodexAsync(executable, ["plugin", "marketplace", "add", preview.PluginDirectory, "--json"], cancellationToken);
            if (!marketplace.Success)
            {
                return new(false, "The plugin files are ready, but Codex did not confirm adding the local marketplace. Use the displayed manual commands or review Codex settings.", status);
            }

            var install = await RunCodexAsync(executable, ["plugin", "add", PluginId, "--json"], cancellationToken);
            if (!install.Success)
            {
                return new(false, "The local marketplace was added, but plugin installation was not confirmed. Open MailMeUp in Codex's plugin settings to complete setup.", status);
            }

            var refreshed = await GetStatusAsync(cancellationToken);
            return new(refreshed.IsPluginConfigured, refreshed.IsPluginConfigured
                ? "The MailMeUp plugin is installed and enabled. Start a new Codex task to load its tools."
                : "Codex accepted the installation request, but enabled configuration could not be confirmed. Refresh the status or inspect the plugin in Codex.", refreshed);
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
            logger.LogWarning("Codex plugin installation stopped with {FailureType}.", exception.GetType().Name);
            var status = State("InstallationIncomplete", "Setup did not finish. Some local plugin files or marketplace configuration may already exist. Refresh status before retrying.");
            return new(false, status.Message, status);
        }
        finally
        {
            installationLock.Release();
        }
    }

    private async Task<CodexSetupPreview> PreparePluginCoreAsync(CancellationToken cancellationToken)
    {
        var preview = GetPreview();
        var source = Path.Combine(AppContext.BaseDirectory, "CodexPlugin");
        var mcp = new Dictionary<string, object>
        {
            ["mailmeup"] = new
            {
                command = preview.StableExecutablePath,
                args = new[] { "--stdio" },
                env = new Dictionary<string, string> { ["MAILMEUP_DATA_DIR"] = ResolveDataDirectory() }
            }
        };
        var mcpContents = JsonSerializer.Serialize(mcp, JsonOptions);
        const string manifestRelativePath = "plugins/mailmeup/.codex-plugin/plugin.json";
        var manifestContents = await File.ReadAllTextAsync(Path.Combine(source, manifestRelativePath), cancellationToken);
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(manifestContents)?.AsObject()
            ?? throw new InvalidDataException("The bundled plugin manifest is missing.");
        var version = manifest["version"]?.GetValue<string>()
            ?? throw new InvalidDataException("The bundled plugin manifest has no version.");
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(manifestContents + mcpContents)))[..16];
        manifest["version"] = $"{version.Split('+')[0]}+codex.{fingerprint}";
        const string marketplaceRelativePath = ".agents/plugins/marketplace.json";
        var marketplace = await File.ReadAllTextAsync(Path.Combine(source, marketplaceRelativePath), cancellationToken);

        await WriteOwnedFileAsync(preview.PluginDirectory, "plugins/mailmeup/.mcp.json", mcpContents, cancellationToken);
        await WriteOwnedFileAsync(preview.PluginDirectory, manifestRelativePath, manifest.ToJsonString(JsonOptions), cancellationToken);
        await WriteOwnedFileAsync(preview.PluginDirectory, marketplaceRelativePath, marketplace, cancellationToken);
        return preview;
    }

    private static async Task WriteOwnedFileAsync(string root, string relative, string contents, CancellationToken cancellationToken)
    {
        var destination = Path.Combine(root, relative);
        var parent = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(parent);
        var temporary = Path.Combine(parent, $".mailmeup-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporary, contents, new UTF8Encoding(false), cancellationToken);
            File.Move(temporary, destination, true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private async Task<CommandResult> RunCodexAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new IOException("Codex could not be started.");
        process.StandardInput.Close();
        var output = ReadBoundedOutputAsync(process.StandardOutput, timeout.Token);
        var errors = ReadBoundedOutputAsync(process.StandardError, timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            var stdout = await output;
            await errors;
            logger.LogInformation("Codex configuration command completed with exit code {ExitCode}.", process.ExitCode);
            return new(process.ExitCode == 0 && stdout is not null, stdout ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                logger.LogWarning("The canceled Codex process could not be terminated: {FailureType}.", exception.GetType().Name);
            }

            try
            {
                await Task.WhenAll(output, errors);
            }
            catch (OperationCanceledException)
            {
                // Both redirected streams are observed before their process is disposed.
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException("Codex configuration command exceeded its time limit.");
        }
    }

    private static async Task<string?> ReadBoundedOutputAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        const int maximumCharacters = 1024 * 1024;
        var text = new StringBuilder();
        var buffer = new char[4096];
        var exceeded = false;
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
        {
            if (text.Length + count <= maximumCharacters)
            {
                text.Append(buffer, 0, count);
            }
            else
            {
                exceeded = true;
            }
        }

        return exceeded ? null : text.ToString();
    }

    private static bool TryReadDirectRegistration(string output, out bool exists)
    {
        exists = false;
        using var document = JsonDocument.Parse(output);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var server in document.RootElement.EnumerateArray())
        {
            if (!TryString(server, "name", out var name))
            {
                return false;
            }

            if (string.Equals(name, "mailmeup", StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
            }

            if (server.TryGetProperty("transport", out var transport) && TryString(transport, "command", out var command)
                && (string.Equals(Path.GetFileName(command), "mailmeup.exe", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(command), "mailmeup", StringComparison.OrdinalIgnoreCase)))
            {
                exists = true;
            }

            if (server.TryGetProperty("transport", out transport) && transport.ValueKind == JsonValueKind.Object
                && transport.TryGetProperty("args", out var arguments) && arguments.ValueKind == JsonValueKind.Array
                && arguments.EnumerateArray().Any(argument => argument.ValueKind == JsonValueKind.String
                    && string.Equals(Path.GetFileName(argument.GetString()), "mailmeup.dll", StringComparison.OrdinalIgnoreCase)))
            {
                exists = true;
            }
        }

        return true;
    }

    private static bool TryReadPluginState(string output, out bool installed, out bool enabled, out bool otherPlugin)
    {
        installed = false;
        enabled = false;
        otherPlugin = false;
        using var document = JsonDocument.Parse(output);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("installed", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!TryString(entry, "pluginId", out var id) || !TryString(entry, "name", out var name))
            {
                return false;
            }

            if (string.Equals(id, PluginId, StringComparison.Ordinal))
            {
                if (!entry.TryGetProperty("enabled", out var enabledValue)
                    || enabledValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return false;
                }

                installed = true;
                enabled = enabledValue.GetBoolean();
            }
            else if (string.Equals(name, "mailmeup", StringComparison.OrdinalIgnoreCase))
            {
                otherPlugin = true;
            }
        }

        return true;
    }

    private static bool TryReadMarketplaceState(string output, string expectedRoot, out bool collision)
    {
        collision = false;
        using var document = JsonDocument.Parse(output);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("marketplaces", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!TryString(entry, "name", out var name))
            {
                return false;
            }

            if (name == MarketplaceName)
            {
                if (!TryString(entry, "root", out var root) || !Path.IsPathFullyQualified(root))
                {
                    return false;
                }

                collision = !string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)),
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(expectedRoot)), StringComparison.OrdinalIgnoreCase);
            }
        }

        return true;
    }

    private static bool TryString(JsonElement value, string property, out string text)
    {
        text = string.Empty;
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(property, out var item) || item.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        text = item.GetString()!;
        return true;
    }

    private static string? FindCodexExecutable()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var unquoted = directory.Trim('"');
            if (!Path.IsPathFullyQualified(unquoted))
            {
                continue;
            }

            var candidate = Path.Combine(unquoted, "codex.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var npm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "node_modules", "@openai");
        foreach (var architecture in new[] { "x64", "arm64" })
        {
            var target = architecture == "x64" ? "x86_64-pc-windows-msvc" : "aarch64-pc-windows-msvc";
            string[] packageRoots = [Path.Combine(npm, "codex"), Path.Combine(npm, "codex", "node_modules", "@openai", $"codex-win32-{architecture}")];
            foreach (var packageRoot in packageRoots)
            {
                var candidate = Path.Combine(packageRoot, "vendor", target, "codex", "codex.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string ResolveDataDirectory() => DataDirectory.Resolve(Environment.GetEnvironmentVariable("MAILMEUP_DATA_DIR"));

    private static string QuotePowerShell(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static CodexSetupStatus State(string code, string message) => new(code, message, false, false, false);

    private static bool IsExpectedFailure(Exception exception) => exception is IOException or UnauthorizedAccessException
        or JsonException or Win32Exception or TimeoutException or InvalidOperationException or ArgumentException;

    private sealed record CommandResult(bool Success, string Output);
}
