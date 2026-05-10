# ValidationGuard

Typed validation error accumulation for .NET 10 with RFC 6901 JSON Pointer paths and deterministic output ordering.

## Release flow (GitHub Actions + MyGet)

This repository uses the workflow at `.github/workflows/ci-cd-myget.yml`.

### CI

- Triggers on:
  - `pull_request` to `main`
  - `push` to `main`
- Steps:
  1. Restore solution
  2. Build (Release)
  3. Run tests (Release)

### CD (publish to MyGet)

- Triggers on `push` to `main` only.
- Publishes **only when package version changed** in:
  - `src/ValidationGuard/ValidationGuard.csproj`
- Version tags that are checked:
  - `<Version>`
  - `<VersionPrefix>`
  - `<VersionSuffix>`
  - `<PackageVersion>`

If a version change is detected, the pipeline:

1. Packs the NuGet package
2. Uploads `.nupkg` as a workflow artifact
3. Pushes package to MyGet (with duplicate-skip enabled)

## Required GitHub secrets

Configure these repository secrets before publishing:

- `MYGET_SOURCE`  
  Example: `https://www.myget.org/F/<feed-name>/api/v3/index.json`
- `MYGET_API_KEY`

Without these secrets, CI still runs, but the publish step cannot succeed.
