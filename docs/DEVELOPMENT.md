# Development

Requirements: .NET SDK from `global.json`, PowerShell 7 and Python 3.10+. End users will not need these development tools.

Install the repository hook once after cloning:

```powershell
pwsh -NoProfile -File scripts/install-git-hooks.ps1
```

The hook applies `dotnet format` to staged C# files before every commit and stages the resulting style fixes. It stops when a C# file is only partially staged so it cannot include unrelated edits accidentally.

```powershell
pwsh -NoProfile -File scripts/validate.ps1
```

This restores locked dependencies, applies formatting, builds with warnings as errors, runs tests and verifies the real CLI/MCP process. It also checks dependency notices and local document links. No real account credentials are used. Use `scripts/format.ps1 -Check` when a read-only formatting check is needed; CI uses the equivalent validation switch.

To run the built app:

```sh
dotnet run --project src/MailMeUp.Cli -c Release --no-build -- status
```

## Dependency updates

Versions live in `Directory.Packages.props`. After an intentional update:

```powershell
dotnet restore MailMeUp.slnx --force-evaluate
python scripts/export-notices.py
pwsh -NoProfile -File scripts/update-portable-locks.ps1
```

Review the standard lock files and `eng/locks/<runtime>/`. Portable builds have separate graphs because they add runtime targets and packaging tools.

Use `MAILMEUP_DATA_DIR` for isolated experiments. Keep code and documents in English. Provider features remain read-only; local metadata storage can still write.

See [contribution guidance](../CONTRIBUTING.md) and [release steps](RELEASING.md).
