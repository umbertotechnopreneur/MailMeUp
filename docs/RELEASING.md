# Releases

GitHub distributes source and executable archives. There is no hosted deployment or marketplace.

## Packages

Windows, Linux and macOS each have x64 and ARM64 targets. Windows uses ZIP; Unix uses tar.gz.

Only Windows x64 is tested for the current MVP. Windows ARM64 is build-only. macOS and Linux automation is retained for future use, but those packages are untested and no support is claimed until suitable machines are available.

Each package includes the application, .NET runtime, license notices, version/commit information and a SHA-256 checksum. Linux targets use glibc, not musl/Alpine.

A native smoke test runs when the runner matches the target CPU. Other packages explicitly record that native execution was not tested.

## Rehearse

Run **Actions > Portable release > Run workflow** on `main`. It builds six archives as workflow artifacts, without creating a release.

Local Windows example, from a clean committed checkout:

```powershell
pwsh -NoProfile -File scripts/package.ps1 -Runtime win-x64
```

Output is under `artifacts/`. Existing packages are not overwritten.

## Publish later

1. Update version, changelog and validation evidence.
2. Merge passing code into `main`.
3. Only when authorized, push the matching `v<Version>` tag.
4. Review the generated draft release and manually publish it.

Dispatch never publishes a release. A tag must match the project version and point to code reachable from `main`.

## Current limits

The foundation is read-only and cannot access real accounts yet. Binaries are unsigned; Authenticode, Apple notarization and attestations are not configured. Checksums detect changes but are not publisher signatures.

No provider credentials belong in builds or release artifacts.
