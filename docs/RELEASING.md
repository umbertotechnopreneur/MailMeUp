# Release process

GitHub distributes source and portable executable archives. There is no marketplace or hosted deployment. CI validates code; the release workflow publishes binaries to artifacts and creates a **draft** release only for a version tag.

## Runtime matrix

| Runtime | Archive | Packaging host |
| --- | --- | --- |
| win-x64 | ZIP | Windows |
| win-arm64 | ZIP | Windows |
| linux-x64 | tar.gz | Linux |
| linux-arm64 | tar.gz | Linux |
| osx-x64 | tar.gz | macOS |
| osx-arm64 | tar.gz | macOS |

Packages are self-contained .NET 10, single-file application bundles with native library extraction, plus README, license texts, dependency notices and `BUILD_INFO.txt`. End users need neither the SDK nor Python. Linux targets use glibc; musl/Alpine is not included. Standard .NET native OS dependencies still apply.

Native smoke tests run when runner OS/CPU match the target. Cross-architecture packages are compiled and explicitly marked `SmokeTest=not-run-cross-architecture`; compilation is not evidence of native execution. Before claiming public platform support, test installation and future OS credential integration on each target.

## Rehearse

Use **Actions → Portable release → Run workflow** on `main`. This runs validation and builds all six archives as workflow artifacts without creating a tag or release. Locally, from a clean committed checkout on the target OS:

```powershell
pwsh -NoProfile -File scripts/package.ps1 -Runtime win-x64
```

The output lives under ignored `artifacts/`. The script refuses to overwrite an existing package directory or archive. Move a previous result aside before rerunning the same version/runtime.

## Cut a release

1. Set the version in `Directory.Build.props`; update the changelog, documentation and dependency inventory.
2. Merge a reviewed, passing commit into `main` and finish native/manual validation for the release scope.
3. When release publication is authorized, create and push the exact `v<Version>` tag (for example `v0.1.0-alpha.1`).
4. The workflow verifies the tag matches the project version and its commit is reachable from `main`, then validates and packages.
5. Review the generated draft release, checksums, notices and platform evidence. Mark prereleases as appropriate before manually publishing.

Workflow dispatch never publishes a release. Release reruns do not overwrite an existing draft automatically; inspect the failed run and existing draft before deciding how to resume.

## Supply chain and signing

Actions are pinned by commit; NuGet dependencies are locked. Archives have SHA-256 sidecars and include the Git commit. A checksum detects file changes but is not a publisher signature. Authenticode signing, Apple notarization and build attestations are not configured in this foundation. Document those gaps for public previews and add signing only after the maintainer chooses the required certificates and process.

No OAuth client secrets, tokens or real account data belong in Actions secrets or build artifacts. Future account authorization happens on each user's machine.
