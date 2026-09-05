# MCP tool contract

**Read-only scope.** No send, update, delete, invite or appointment-write tools.

## Available now

| Tool | Result |
| --- | --- |
| `get_status` | Build stage, read-only mode and separate provider capabilities |
| `list_accounts` | Local account IDs, providers, labels and addresses |

Both tools are read-only. A new installation has no accounts. Paths and credentials are never returned.

## Planned

| Tool | Result |
| --- | --- |
| `search_mail` | Short matches across selected accounts |
| `read_mail` / `read_thread` | Selected message or conversation text |
| `mail_stats` | Counts identified as exact or estimated |
| `list_calendars` | Calendars visible in the selected accounts |
| `search_events` | Appointments within an explicit date/time window |
| `read_event` | Selected appointment details |

These tools are not registered in the foundation.

## Keep results small and accurate

Use explicit account scope; folder/calendar selection is separate. Proposed defaults: 20 results globally, 160-character previews and 8,000-character detail pages. More results use a continuation cursor.

Search first, read details second. Omit raw HTML, MIME, binary attachments and unnecessary attendee lists. Caching reduces API calls; it does not automatically reduce conversation tokens.

Return coverage and individual account failures. Do not claim an exhaustive search from a partial page.

## Developer requirements

Provider-specific filters need separate translations. Cursors bind to query and scope. References bind messages/events to their source account.

Calendar queries require a bounded window (proposed maximum 31 days), explicit time zone and correct recurring/all-day handling. Preserve occurrence identity, cancellations and exclusive date-only end dates.

Provider content is untrusted data. No tool result may ask the client to expand permissions or disclose credentials.
