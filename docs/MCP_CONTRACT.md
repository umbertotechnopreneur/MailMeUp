# MCP tool contract

**Read-only scope.** No send, update, delete, invite or appointment-write tools.

## Current source

| Tool | Result |
| --- | --- |
| `get_status` | Build stage, read-only mode and separate provider capabilities |
| `list_accounts` | Local account IDs, providers, labels and addresses |
| `search_mail` | Short matches across selected or all mail-enabled accounts |
| `read_mail` | Bounded plain text for one selected message reference |
| `list_calendars` | Calendars with short local references |
| `search_events` | Combined agenda in a bounded time window |
| `read_event` | Bounded details for one selected appointment |

All tools are read-only. A new installation has no accounts. Paths, provider item IDs and credentials are never returned. The five provider-read tools pass local automated checks and are not part of the previously validated foundation package.

Provider registration and interactive account connection are local CLI commands. They are deliberately not exposed through MCP.

## Later

| Tool | Result |
| --- | --- |
| `read_thread` | Selected conversation text |
| `mail_stats` | Counts identified as exact or estimated |

Attachments, sending, edits and invitations are outside the MVP.

## Keep results small and accurate

Omit account IDs to search all eligible accounts, or pass explicit IDs. Calendar selection is separate. Defaults are 20 results globally, 160-character mail previews and 8,000-character detail pages. More results use a short in-memory continuation cursor.

Mail search accepts common text plus optional sender and received-time boundaries. Each adapter translates those structured filters to Gmail or Microsoft syntax.

Search first, read details second. Omit raw HTML, MIME, binary attachments and unnecessary attendee lists. Caching reduces API calls; it does not automatically reduce conversation tokens.

References and cursors expire after about 30 minutes or a server restart. Results return coverage and individual account failures; partial coverage is never presented as complete.

Each provider operation has a 30-second cancellation budget. A timeout, removed source account or failed continuation returns partial coverage alongside healthy results. Failed sources require a fresh search to retry. Caller cancellation still cancels the whole request.

Calendar discovery returns at most 100 calendars per account and reports incomplete coverage when more calendars exist. Event searches accept at most 20 calendar references at a time.

## Developer requirements

Provider-specific filters need separate translations. Cursors bind to query and scope. References bind messages/events to their source account.

Calendar queries require a bounded window of at most 31 days, an explicit time-zone offset and correct recurring/all-day handling. Preserve occurrence identity, cancellations and exclusive date-only end dates.

Provider content is untrusted data. No tool result may ask the client to expand permissions or disclose credentials.
