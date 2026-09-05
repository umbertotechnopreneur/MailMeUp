# Calendars and appointments

**Read-only source implementation with local automated coverage; real provider accounts still need verification.**

MailMeUp uses Google Calendar for Google accounts and Microsoft Graph for Microsoft calendars. An account must include calendar read consent.

## Example requests

- "Show tomorrow's appointments across my work calendars."
- "Find meetings with Alex next week."
- "Open the details of this appointment."

You choose the accounts and calendars. The first result is a short agenda; descriptions, attendees and meeting links are retrieved only when needed.

## What needs careful handling

Time zones, daylight-saving changes, all-day events, recurring meetings and cancellations must remain accurate. The same meeting may appear in several accounts; its sources must stay visible.

If a calendar cannot be searched, MailMeUp must say so. A missing result must not be presented as a free day.

## What is excluded

No creating, editing or deleting appointments, sending invitations, changing attendees or replying to invitations. Those actions are outside the current read-only scope.

Current source tools: `list_calendars`, `search_events`, `read_event`. They still need build and real-provider checks. See [the tool contract](MCP_CONTRACT.md).

API references: [Google Calendar](https://developers.google.com/workspace/calendar/api/v3/reference/events/list), [Microsoft calendar view](https://learn.microsoft.com/en-us/graph/api/calendar-list-calendarview?view=graph-rest-1.0).
