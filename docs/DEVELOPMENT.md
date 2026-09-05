# Development

Use .NET SDK 10.0.400 (latest patch roll-forward), PowerShell 7 and Python 3.10+. Package versions are central in `Directory.Packages.props`; commit each `packages.lock.json`. `NuGet.Config` uses only nuget.org.

```powershell
pwsh -NoProfile -File scripts/validate.ps1
```

The script restores in locked mode, verifies formatting, builds with warnings as errors, runs tests, starts the real CLI/MCP process, verifies the dependency inventory and checks local documentation links/obvious accidental data inclusion. It never contacts a mailbox or uses a real user registry.

After intentionally changing packages:

```sh
dotnet restore MailMeUp.slnx --force-evaluate
python scripts/export-notices.py
dotnet format MailMeUp.slnx --no-restore
```

Review lock files and `docs/DEPENDENCIES.md`, then run the full validation script. `scripts/repo-check.py` is a lightweight local-data check, not a comprehensive secret scanner or security audit.

CI runs the same validation on Windows, Linux and macOS. Use `MAILMEUP_DATA_DIR` for any runtime experiment that needs storage. Provider tests must be opt-in and isolated when introduced; ordinary CI must remain credential-free.

`MailMeUp.slnx` can be opened with tooling that supports the modern solution format. The SDK builds it directly; no IDE is required.
