namespace MailMeUp.Core;

/// <summary>Non-secret account metadata. Token material never belongs here.</summary>
public sealed record Account(
    string Id,
    string Provider,
    string DisplayName,
    string EmailAddress,
    bool MailReadEnabled = false,
    bool CalendarReadEnabled = false);
