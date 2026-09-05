# Roadmap

Milestones are capability gates, not delivery dates.

| Milestone | Deliverable | Acceptance gate |
| --- | --- | --- |
| 0 — Foundation | English docs, MIT repo, brand, solution, CLI, discovery MCP, metadata, CI/CD | Build, account integrity tests and stdio smoke pass |
| 1 — Credentials and account setup | Google desktop OAuth, Microsoft public-client MSAL, named accounts, OS token storage, disconnect | Multiple accounts survive restart; no plaintext fallback; refresh/revocation/isolation tests |
| 2 — Provider reads | Gmail and Graph search/read adapters | Real consented test accounts on both providers; portable filters and provider errors verified |
| 3 — Unified tools | `search_mail`, `read_mail`, `read_thread`, `mail_stats` | Global limits, continuation, partial coverage and account attribution tested |
| 4 — Public read-only preview | Native installation guides, distribution auth decision, user onboarding | Per-platform credential tests, provider policy readiness and clean installation tests |
| Later | Drafts, send, attachments, optional sync | Separate scope, permissions and user-confirmation design |

## Next implementation sequence

1. Implement and test OS-protected storage first, including locked/unavailable vault behavior.
2. Add public OAuth client settings and interactive `accounts add` / `accounts remove` commands.
3. Bind local account IDs to verified provider identities and protected credential references.
4. Implement Google sign-in/refresh, then Microsoft MSAL sign-in/cache integration.
5. Add minimal provider search and selective reads before building the unified coordinator.

Marketplace distribution is not part of the roadmap. A hosted service or web interface would require a separate design.
