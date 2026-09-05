# Account recovery

**Pre-alpha, read-only.** Recovery changes passed synthetic Windows tests, including failed reconnect and cross-process credential locking. Deliberate real revocation and reconnect remain untested.

- **Temporary error:** repeat the search. Healthy accounts can still return results; check the reported coverage before treating an empty result as no mail or appointments.
- **Expired or revoked authorization:** run `mailmeup accounts connect google` or `mailmeup accounts connect microsoft` and select the same account. This updates its local connection. Keep the read categories you still need.
- **Local removal:** use `mailmeup accounts list`, then `mailmeup accounts remove <account-id>`. This removes the local account and its cached credentials. It does not change mail, calendar data or the provider grant.
- **Credentials unavailable:** check access to the operating-system credential store and that CLI and MCP use the same `MAILMEUP_DATA_DIR`.

Refresh and removal coordinate across local processes running the updated build. Restart older CLI/MCP processes before checking this behavior; older builds do not use the new session locks. An already running read may finish; subsequent reads require an available account and credentials. A failed search continuation stops using the failed source; start a new search after recovery.

Next, check real token expiry, revoked access and recovery. Use synthetic data for removal/refresh fault simulation. Real-account checks are manual and require at least two connected accounts; see [validation](VALIDATION.md).
