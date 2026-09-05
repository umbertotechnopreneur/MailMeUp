namespace MailMeUp.Core;

/// <summary>Non-secret account metadata. Provider identity and token material never belong here.</summary>
public sealed record Account(string Id, string Provider, string DisplayName, string EmailAddress);
