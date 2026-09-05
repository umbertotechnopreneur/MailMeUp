using System.ComponentModel;
using System.Text.Json;
using MailMeUp.Application;
using ModelContextProtocol.Server;

namespace MailMeUp.Mcp;

/// <summary>Small read-only discovery tools; mail tools are registered only when implemented.</summary>
[McpServerToolType]
public sealed class MailTools(IMailMeUpApplication application)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>Reports readiness without disclosing local paths or credentials.</summary>
    [McpServerTool(Name = "get_status", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Report MailMeUp readiness. This foundation cannot authenticate accounts, read mail or access calendars yet.")]
    public JsonElement GetStatus() => JsonSerializer.SerializeToElement(application.GetStatus(), JsonOptions);

    /// <summary>Lists local account metadata without reading message contents.</summary>
    [McpServerTool(Name = "list_accounts", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("List locally registered account IDs, providers, labels and email addresses. Empty on a new installation. No tokens or message bodies.")]
    public async Task<JsonElement> ListAccountsAsync(CancellationToken cancellationToken = default) =>
        JsonSerializer.SerializeToElement(new { Accounts = await application.ListAccountsAsync(cancellationToken) }, JsonOptions);
}
