# Calendar application boundary

The shared application facade now provides calendar discovery, bounded multi-account event search and selected-event reads. It reuses account resolution and partial-failure reporting while keeping calendar windows and provider continuations separate. See `docs/CALENDARS.md` at the repository root.
