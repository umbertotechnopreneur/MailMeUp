# Microsoft calendar adapter boundary

Microsoft Graph calendar discovery, chronological `calendarView` queries and selected-event details use delegated `Calendars.Read` access. The adapter reuses MSAL identity and the protected multi-account cache. Provider writes are absent.
