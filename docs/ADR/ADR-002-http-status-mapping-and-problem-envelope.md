# ADR-002: HTTP status mapping and Problem Details envelope selection

- Status: Accepted
- Date: 2026-10-14
- Deciders: ValidationGuard maintainers

## Context

`ValidationEntry` is transport-agnostic: it carries `pointer`, `code`, `format`, `detail` and optional metadata, but no HTTP status. An HTTP response carries exactly one status, `type` and `title`, while a validation run produces N entries. The HTTP layer therefore has to fold N entries into one envelope.

Constraints that shaped the decision:

- Error codes are short, origin-oriented identifiers such as `IDX001`. They are defined by ValidationGuard packages and by library customers, so the set is open and two vendors can pick the same code.
- Different transports partition errors differently. gRPC collapses HTTP 400 and 422 into `INVALID_ARGUMENT` but separates `FAILED_PRECONDITION` from `ABORTED`. A transport-neutral "kind" vocabulary derived from HTTP statuses would be lossy for other transports.
- The `errors` extension always carries every entry with its `code`, so a missing status mapping degrades only the envelope, never the information clients need.
- Validating mappings at application startup repeats the same deterministic check on every replica start without ever finding something new. Checks that depend only on compiled code belong in the build.
- Building a response is on the request path, so avoidable allocations matter.

## Decision

1. **A separate `ValidationGuard.Http` package owns HTTP classification.** The core package stays free of HTTP concepts. A future gRPC package would own its own `code → status` mapping.
2. **Mappings are flat: `code → (status, type, title)`.** No intermediate kind vocabulary. `type` and `title` are optional.
3. **Mappings are declared in assembly metadata** with `[assembly: HttpErrorCode("IDX001", 422, Type = "...", Title = "...")]`. Metadata is visible to analyzers in every compilation that references the declaring assembly, which a registration method body is not.
4. **Overrides are explicit** with `[assembly: HttpErrorCodeOverride(...)]` or `ErrorCodeMapBuilder.Override(...)`. Adding a code that is already registered throws; only `Override` replaces a mapping. Overrides are applied when the map is built, so registration order does not matter, and building throws if an overridden code was never added.
5. **Status precedence is a ranking over statuses, not codes.** The default is `403 > 404 > 409 > 422 > 400`; unlisted statuses rank lowest. Ranking statuses keeps every contributor's codes local: no contributor has to know how its codes rank against another package's codes. The application can register its own `StatusPrecedence`.
6. **Envelope selection.** The response status is the highest-ranked status among all entries. Unmapped codes take the fallback status (422 by default) and are logged.
   - If exactly one entry has the highest-ranked status, the envelope uses that code's declared `type` and `title`, and the entry's `detail` becomes the Problem Details `detail`. A `type` or `title` the code does not declare is left unset.
   - If several entries share the highest rank, whether repeats of one code or different codes with equal-ranked statuses, `type`, `title` and `detail` are left unset.
   - Unset `type` and `title` are filled by ASP.NET Core with its generic, status-derived values when the response is written through `TypedResults.Problem`, `Results.Problem` or `IProblemDetailsService`.
   - Picking one entry's `type` from several would make the envelope depend on which entry happened to be chosen; the generic values describe the aggregate honestly.
   - `title` is declared per code rather than taken from the entry because RFC 9457 §3.1.3 says a title should not change between occurrences of a problem type. The entry's `detail` is rendered per occurrence and may contain request values, so it belongs in the occurrence-specific `detail` member (§3.1.4).
7. **The `errors` extension is always present** and lists every entry. Each entry is written as `pointer`, `code`, `format`, `detail`, plus a nested `metadata` object when metadata is non-empty. Nesting keeps metadata keys from colliding with the four contract fields.
8. **Entries are serialized in place.** The factory stores a `ValidationErrors` wrapper over the entry list in `ProblemDetails.Extensions`; its JSON converter writes each entry directly with pre-encoded property names. No per-entry dictionaries or projection objects are created.
9. **No validation at startup beyond building the map.** Building the map still throws on a code added twice and on an override whose code was never added, because those checks are inherent to constructing it. Everything else is checked at build time. A bundled Roslyn analyzer, shipped in the same NuGet package under `analyzers/dotnet/cs`, enforces mapping rules at build time:

| Rule | Severity | Check |
|------|----------|-------|
| VGH001 | Error | Code is null, empty or whitespace |
| VGH002 | Error | Status is outside 400–599 |
| VGH003 | Error | `type` is not an absolute, non-file URI |
| VGH004 | Error | Code declared twice in the same assembly |
| VGH005 | Warning | Code declared by this assembly and a referenced one, or by two referenced assemblies |
| VGH006 | Warning | Override targets a code no visible assembly declares |
| VGH007 | Error | Code overridden twice in the same assembly |

VGH001–VGH003 also apply to constant arguments of `ErrorCodeMapBuilder.Add` and `Override`.

VGH005 is a warning, not an error: the analyzer sees every referenced assembly, but it cannot know whether the application registers both of the colliding ones. Registering both throws when the map is built at startup.

### Allocation profile

Per response: the `ProblemDetails` instance and its framework-owned `Extensions` dictionary, plus one `ValidationErrors` wrapper. The fold loops by index over `IReadOnlyList<ValidationEntry>` with no enumerator, LINQ or per-entry allocation. Map lookups use a `FrozenDictionary`, and status ranks are an array indexed by status. The unmapped-code warning uses a source-generated `LoggerMessage`, so it formats nothing when the level is disabled.

## Consequences

### Positive

- Mapping mistakes fail the build of the assembly that makes them, and code collisions between packages surface as warnings in the application's build.
- `title` stays stable per problem type, so log aggregation and dashboards grouped by title stay meaningful, and occurrence-specific text never leaves the `detail` member.
- Startup cost is limited to reading the declaring assemblies' attributes and building one frozen map.
- Codes contributed by different packages compose without any package knowing about the others.
- Clients get one stable contract: branch on `errors[].code`; treat status, `type` and `title` as a summary.

### Negative / Trade-offs

- Code collisions across vendors cannot be resolved by overriding; an application that registers two assemblies declaring the same code fails at startup and must drop one of them or have a vendor rename the code.
- Mappings registered through `ErrorCodeMapBuilder.Add` with non-constant arguments are not checked by the analyzer.
- Generic `type` and `title` are applied when the response is written through ASP.NET Core result or problem-details services. Serializing the `ProblemDetails` directly leaves them unset.
- Every code needs its own declaration; there is no default classification to inherit.
- The analyzer targets Roslyn 4.8, so SDKs older than .NET 8 do not load it.

## Alternatives Considered

1. Add a status to `ValidationEntry` or carry it in metadata.
   - Rejected: couples the core to HTTP, and metadata would leak a transport field into every `errors` entry.
2. Transport-neutral failure kinds mapped per transport.
   - Rejected for now: with one transport, the vocabulary would be HTTP statuses under other names. It can be introduced if a second transport appears.
3. Registration in DI extension methods with startup validation.
   - Rejected: method bodies are invisible to analyzers in consuming compilations, and startup validation repeats a deterministic check on every replica start.
4. Precedence ranking over codes.
   - Rejected: requires global knowledge of every package's codes to place a new code.
5. Per-code `type` and `title` regardless of entry count.
   - Rejected: the envelope would change shape depending on how many fields failed.
6. The top entry's `detail` as the response `title`.
   - Rejected: the title would vary per occurrence, contrary to RFC 9457 §3.1.3, and could carry request values into a field consumers treat as a stable label.
7. Building `Dictionary<string, object?>` per entry for the `errors` extension.
   - Rejected: N allocations per response for data that can be written straight from the entries.
