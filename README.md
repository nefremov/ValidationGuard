# ValidationGuard

Typed validation error accumulation for .NET 10 with RFC 6901 JSON Pointer paths and deterministic output ordering.

## Packages

| Package | Purpose |
|---------|---------|
| `ValidationGuard` | Transport-agnostic validation builder and entries |
| `ValidationGuard.Http` | RFC 9457 Problem Details projection for ASP.NET Core, with a bundled build-time analyzer |

## HTTP responses with ValidationGuard.Http

Libraries that define validation codes declare each code's HTTP status, and optionally a problem `type` and a stable `title`, in assembly metadata:

```csharp
using ValidationGuard.Http;

[assembly: HttpErrorCode("IDX001", 422)]
[assembly: HttpErrorCode("IDX017", 409, Type = "https://docs.acme.com/errors/IDX017", Title = "SKU retired")]

public sealed class AcmeCodes;
```

The application registers the declaring assemblies and may override individual mappings, either with `[assembly: HttpErrorCodeOverride(...)]` in its own assembly or in code:

```csharp
builder.Services.AddValidationProblems(codes => codes
    .AddAssemblyOf<AcmeCodes>()
    .AddAssemblyOf<Program>()
    .Override("IDX017", StatusCodes.Status422UnprocessableEntity));
```

Endpoints turn the root builder's entries into a response:

```csharp
app.MapPost("/orders", (Order order, ValidationProblemFactory problems) =>
{
    using var validation = ValidationBuilder<Order>.Create(ValidationBehavior.CamelCase);
    // ... add entries ...
    var entries = validation.ToValidationEntries();
    return entries.Count == 0 ? Results.Ok() : (IResult)problems.CreateResult(entries);
});
```

How the envelope is chosen:

- The status is the highest-ranked status among the entries' codes (default ranking `403 > 404 > 409 > 422 > 400`; register your own `StatusPrecedence` singleton to change it). Unmapped codes fall back to 422 and are logged.
- If exactly one entry holds that status, the response uses its code's declared `type` and `title`, and the entry's `detail` becomes the response `detail`. Anything not declared falls back to ASP.NET Core's generic values.
- If several entries hold it, ASP.NET Core's generic `type` and `title` for the status are used and `detail` is omitted.
- The `errors` extension always lists every entry as `pointer`, `code`, `format`, `detail`, plus `metadata` when present. Clients should identify failures by `errors[].code`.

The package includes an analyzer that checks declarations at build time:

| Rule | Severity | Check |
|------|----------|-------|
| VGH001 | Error | Empty code |
| VGH002 | Error | Status outside 400–599 |
| VGH003 | Error | `Type` is not an absolute URI |
| VGH004 | Error | Code declared twice in one assembly |
| VGH005 | Warning | Code declared by more than one assembly |
| VGH006 | Warning | Override of a code no referenced assembly declares |
| VGH007 | Error | Code overridden twice in one assembly |

See [ADR-002](docs/ADR/ADR-002-http-status-mapping-and-problem-envelope.md) for the reasoning.

## Release flow (GitHub Actions + NuGet)

This repository uses the workflow at `.github/workflows/ci-cd-nuget.yml`.

### CI

- Triggers on:
  - `pull_request` to `main`
  - `push` to `main`
- Steps:
  1. Restore solution
  2. Build (Release)
  3. Run tests (Release)

### CD (publish to nuget.org)

- Triggers on `push` to `main` only.
- Publishes **only when a package version changed**:
  - A change to the shared version in `src/Directory.Build.props` publishes every package.
  - A version override added to an individual project file publishes only that package:
    - `src/ValidationGuard/ValidationGuard.csproj`
    - `src/ValidationGuard.Http/ValidationGuard.Http.csproj` (the package includes the bundled analyzer)
- Version tags that are checked:
  - `<Version>`
  - `<VersionPrefix>`
  - `<VersionSuffix>`
  - `<PackageVersion>`

If a version change is detected, the pipeline:

1. Packs each package whose version changed, with its symbol package (`.snupkg`)
2. Uploads the `.nupkg` and `.snupkg` files as a workflow artifact
3. Exchanges the job's GitHub OIDC token for a short-lived nuget.org API key via [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
4. Pushes the packages to nuget.org (with duplicate-skip enabled)

`ValidationGuard.Http` depends on the `ValidationGuard` package version it was built against. Bumping the shared version keeps the two in lockstep; if you override a single project's version instead, bump and publish `ValidationGuard` first, or in the same commit, when the Http package needs core changes.

## Build configuration

- `tests/Directory.Build.props` holds settings shared by the test projects.
- `src/Directory.Build.props` holds the single package version and the shared package metadata for every shipping project under `src/`. Test projects live outside `src/` and are not versioned.
- `src/Directory.Build.targets` packs `README.md` and `LICENSE` into every packable project.
- `src/Directory.Packages.props` and `tests/Directory.Packages.props` pin NuGet dependency versions (central package management) for their folder. `PackageReference` items in project files carry no `Version`. `Microsoft.CodeAnalysis.CSharp` appears in both and must stay in sync, because the analyzer tests compile against the same Roslyn version as the analyzer.

## Publishing setup

No long-lived API key is stored in the repository. Publishing requires:

- A **Trusted Publishing policy** on nuget.org naming owner `nefremov`, repository `ValidationGuard`, and workflow file `ci-cd-nuget.yml`. Renaming the workflow file breaks publishing until the policy is updated.
- A repository secret **`NUGET_USER`** containing the nuget.org profile name (not email) that owns the policy.

Without these, CI still runs, but the publish step cannot succeed.
