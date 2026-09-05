# MCP contract

## Implemented surface

| Tool | Input | Output |
| --- | --- | --- |
| `get_status` | `{}` | Stage, transport, provider readiness, `can_connect_accounts` |
| `list_accounts` | `{}` | `accounts`: local ID, provider, display name, email address |

Both tools are annotated read-only, non-destructive and closed-world. No path or token fields are exposed. A fresh install returns `{"accounts":[]}`. These annotations describe behavior; they are not a substitute for authorization.

## Planned mail surface

These are design proposals, **not registered tools in the foundation**.

| Tool | Main inputs | Bounded output |
| --- | --- | --- |
| `search_mail` | Account scope, folder scope, text/from/date/unread filters, limit, cursor | Compact matches, coverage, per-account errors, continuation |
| `read_mail` | Opaque message reference, character limit, continuation | Headers and selected plain text, truncation indicator |
| `read_thread` | Opaque thread reference, message/character limits | Ordered message headers and bounded bodies |
| `mail_stats` | Account scope and supported filters | Counts with exact/estimated/unsupported attribution |

### Account and folder scope

Use an explicit account selector: `{ "mode": "all" }` or `{ "mode": "selected", "account_ids": ["acc_example"] }`. Missing or invalid selections must fail rather than silently widening the search. Folder scope is separate: inbox, all-mail or selected provider folder IDs. `all` accounts means every configured account, not every mailbox the provider identity could theoretically access.

### Example search proposal

```json
{
  "accounts": { "mode": "all" },
  "folders": { "mode": "all_mail" },
  "filters": { "text": "contract", "received_after": "2026-09-01T00:00:00Z" },
  "limit": 20
}
```

Each match should include `message_ref`, `account_id`, `from`, `to`, `subject`, `received_at` and an optional short `preview`. Limit is global: 20 results across all accounts, not 20 per account. Cap accepted limits (proposed maximum 100) and previews (proposed 160 characters). Include `next_cursor`, `has_more`, selected-account statuses and `partial` separately.

A cursor must bind to the normalized query and selected accounts, retain pending provider pages and expire explicitly. Reject reuse with different filters. Do not return provider access tokens or raw continuation URLs. Sorting and deduplication must remain stable across pages. Provider estimates must never be labeled exact totals.

### Context efficiency

1. Return headers and a short preview from search; omit raw MIME, HTML, binary attachments and base64.
2. Read only selected messages. Convert HTML to plain text, retaining useful links and indicating truncation.
3. Bound body output (proposed default 8,000 characters) and expose continuation rather than silently discarding the remainder.
4. Use compact JSON and one consistent field set; do not rename every field to an unreadable abbreviation.
5. Do not add an LLM inside the server. Codex performs the conversation's reasoning and summaries.

SQLite caching can reduce provider requests and latency. It does not automatically reduce tokens in repeated MCP output. Character limits are output controls, not exact model token counts. Any quoted reply/signature removal must be optional and declared because it can remove meaningful context.

### Provider differences and failures

Portable filters need separate Gmail/Graph translations; arbitrary Gmail query syntax cannot be sent to Graph. Unsupported filter combinations must be explained. Use timeouts, bounded concurrency, cancellation and provider retry guidance. Return available matches alongside per-account failures; distinguish no matches, not authorized, unavailable and not searched. Exhaustive search requires all continuation pages and successful coverage of every selected account.

Email bodies, subjects, links and attachments are untrusted data. They must not instruct the client to invoke tools, disclose credentials or widen account scope.
