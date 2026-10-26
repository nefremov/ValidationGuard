# ValidationGuard

Typed validation error accumulation for .NET 10 with RFC 6901 JSON Pointer paths and deterministic output ordering.

## Packages

| Package | Purpose |
|---------|---------|
| `ValidationGuard` | Transport-agnostic validation builder and entries |
| `ValidationGuard.CommonErrors` | Ready-made errors with stable codes and message formats (not found, null, empty, range, length) |
| `ValidationGuard.Http` | RFC 9457 Problem Details projection for ASP.NET Core, with a bundled build-time analyzer |
| `ValidationGuard.CommonErrors.Http` | HTTP statuses for the `ValidationGuard.CommonErrors` codes |

Runnable examples, including a library that extends ValidationGuard with its own errors, are in [samples](samples/README.md).

## Common errors with ValidationGuard.CommonErrors

Extension methods on any validation builder add frequently needed errors with a stable code, a localizable
`format`, a rendered `detail`, and one `metadata` value per format placeholder. They only report: the caller decides
whether a value is invalid.

```csharp
using ValidationGuard.CommonErrors;

using var validation = ValidationBuilder<CreateOrder>.Create(NameConverters.CamelCase);

if (!customers.Exists(request.CustomerId))
    validation.AddNotFound("Customer", x => x.CustomerId, request.CustomerId);

if (request.Quantity < 1)
    validation.AddLessThan("Order", x => x.Quantity, request.Quantity, 1);
```

produces

```json
{ "pointer": "", "code": "VG0001",
  "format": "{entity} with {name} = {value} does not exist",
  "detail": "Customer with customerId = \"3f2b...e1\" does not exist",
  "metadata": { "entity": "Customer", "name": "customerId", "value": "3f2b...e1" } }
```

| Code | Method | Format | HTTP |
|------|--------|--------|------|
| `VG0001` | `AddNotFound` (1, 2 or 3 key properties) | `{entity} with {name} = {value} does not exist`<br>`{entity} with {name1} = {value1} and {name2} = {value2} does not exist`<br>`{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} does not exist` | 404 |
| `VG0010` | `AddAlreadyExists` (1, 2 or 3 key properties) | `{entity} with {name} = {value} already exists` (and 2/3-key variants) | 409 |
| `VG0011` | `AddNotUnique` (1, 2 or 3 unique properties) | `{entity} with {name} = {value} already exists`<br>`{entity} with {name1} = {value1} and {name2} = {value2} already exists`<br>`{entity} with {name1} = {value1}, {name2} = {value2} and {name3} = {value3} already exists` | 409 |
| `VG0002` | `AddNull` | `Value for {name} is required` | 422 |
| `VG0003` | `AddEmpty` (strings) | `Value for {name} must not be empty` | 422 |
| `VG0004` | `AddLessThan` | `Value {value} for {name} must be at least {min_value}` | 422 |
| `VG0005` | `AddLessThanOrEqual` | `Value {value} for {name} must be greater than {min_value}` | 422 |
| `VG0006` | `AddGreaterThan` | `Value {value} for {name} must be at most {max_value}` | 422 |
| `VG0007` | `AddGreaterThanOrEqual` | `Value {value} for {name} must be less than {max_value}` | 422 |
| `VG0008` | `AddTooShort` (strings) | `Value for {name} must be at least {min_length} characters long, but has {length}` | 422 |
| `VG0009` | `AddTooLong` (strings) | `Value for {name} must be at most {max_length} characters long, but has {length}` | 422 |
| `VG0012` | `AddOutOfRange` (`RangeBounds`: `Inclusive`, `Exclusive`, `MinInclusive`, `MaxInclusive`) | `Value {value} for {name} must be in the range [{min_value}, {max_value}]`, with `(`/`)` for excluded ends | 422 |
| `VG0013` | `AddReferenceNotFound` (foreign keys) | `Value {value} for {name} does not refer to an existing {referenced_entity}` | 422 |
| `VG0014` | `AddIncompatibleValues` (two properties) | `{entity} cannot have {name1} = {value1} when {name2} = {value2}` | 422 |
| `VG0015` | `AddNotAllowed` (a list of values, an enum type, or an `int` holding an enum) | `Value {value} for {name} must be one of {allowed_values}` | 422 |
| `VG0016` | `AddNotModifiable` | `Value for {name} cannot be changed` | 422 |

- Entity errors take the entity first, then one to three key property/value pairs, each with its own type:
  `AddNotFound("OrderLine", x => x.OrderId, orderId, x => x.LineId, lineId)` produces
  `OrderLine with orderId = "3f2b...e1" and lineId = "9c1d...07" does not exist`;
  `AddNotUnique("Product", x => x.Sku, sku, x => x.VendorId, vendorId)` produces
  `Product with sku = "CH-12" and vendorId = "3f2b...e1" already exists`. The key properties are symmetric: the entry points
  at the builder's scope (the entity), and every property's path and value appear in the message and metadata.
  Both "already exists" messages read alike, but the codes differ so clients can tell them apart:
  `AddAlreadyExists` reports a primary-key conflict (`VG0010`), `AddNotUnique` a unique-constraint
  conflict (`VG0011`).
- Every error is about an entity, passed first: property errors too, e.g. `AddNull("Customer", x => x.Email)`. The
  entity is always in `metadata` (`"entity": "Customer"`); the built-in property formats do not show it.
- `{name}` is the property's path from the root in the builder's naming convention, e.g. `customerId`,
  `address.city`, `lines[2].sku` or `tags["a/b"]`; it includes the scope of nested builders.
- Range methods take the offending value and the bound, e.g. `AddLessThan("Order", x => x.Quantity, request.Quantity, 1)`;
  both appear in `detail` and `metadata`. Values in `detail` are formatted with the invariant culture.
- Length methods take the string but report only its `{length}`, so user input never appears in the response.
- `AddOutOfRange("Order", x => x.Quantity, quantity, 1, 100, RangeBounds.MinInclusive)` reports `min <= x < max` as
  `must be in the range [1, 100)`; one code covers all four bound combinations, and the format shows which ends are in.
- `AddReferenceNotFound("Order", x => x.CustomerId, "Customer", id)` points at the referring property (422), unlike
  `AddNotFound`, which reports a missing resource (404).
- `AddIncompatibleValues("Order", x => x.Shipping, shipping, x => x.Country, country)` treats both properties alike and
  points at the entity, like `AddNotUnique`.
- `AddNotAllowed` takes the allowed values (`["CH", "DE"]`) or, for an enum property, none: every defined value of the
  enum is allowed. The detail lists them separated by commas; metadata holds them as an array (`ValueList<T>`).
  For an `int` property holding an enum as its number (any integer can be cast to an enum), pass the enum type:
  `AddNotAllowed("Order", x => x.ShippingCode, code, typeof(Shipping))` allows only the defined numbers.
- Codes and formats are available as constants in `CommonErrorCodes` and `CommonErrorFormats`. The `VG` prefix is
  reserved for ValidationGuard; codes are never renumbered or reused.
- The package depends only on `ValidationGuard`. The HTTP statuses above are provided by
  `ValidationGuard.CommonErrors.Http`; register them with
  `AddValidationProblems(codes => codes.AddCommonErrors())` and change one with
  `.Override(CommonErrorCodes.NotFound, 422)` or `[assembly: HttpErrorCodeOverride(...)]`.

### Defining your own errors

Libraries can define errors with the same conventions as the built-in ones: `{name}` as the property path, typed
culture-invariant rendering, and one metadata value per placeholder. Define each error once as an
`ErrorDefinition` and add it with `validation.Add(...)`:

```csharp
using ValidationGuard.CommonErrors;

public static class AcmeErrors
{
    public static readonly PropertyErrorDefinition<int, int, int> NotBetween = new(
        "ACME0001", "Value {value} for {name} must be between {min} and {max}", "value", "min", "max");

    public static readonly EntityErrorDefinition SlugTaken = new("ACME0002", "{entity} with {name} = {value} is taken");
}

validation.Add(AcmeErrors.NotBetween, "Customer", x => x.Age, request.Age, 18, 65);
// "Value 12 for age must be between 18 and 65"   metadata: entity, name, value, min, max
validation.Add(AcmeErrors.SlugTaken, "Article", x => x.Slug, request.Slug);
// "Article with slug = \"hello-world\" is taken"             metadata: entity, name, value
```

| Type | Built-in placeholders | Other placeholders |
|------|-----------------------|--------------------|
| `EntityErrorDefinition` (1, 2 or 3 key properties) | `{entity}`, passed to `Add` | a `{name}`/`{value}` pair per key property (`{name1}`, `{value1}` ... for 2 or 3), typed by its accessor |
| `PropertyErrorDefinition`, `PropertyErrorDefinition<T1>` ... `<T1, T2, T3>` (derive from `EntityErrorDefinition`) | `{entity}`, passed to `Add`, optional in the format; `{name}`, from the accessor | 0-3 typed arguments with declared keys |

- Every placeholder must be a built-in or declared key, and every key must appear in the format (except a property
  error's `{entity}`). This is not
  checked at run time; verify it once in a unit test, which reports every invalid definition at once:

  ```csharp
  [Fact]
  public void ErrorDefinitions_AreValid() => ErrorDefinitionValidator.ValidateFields(typeof(AcmeErrors));
  ```
- Each argument has its own type, and the call site is checked against it.
- For errors generic over a value type, use a generic holder, as the built-in range errors do with
  `CommonErrorDefinitions.Comparison<T>.LessThan`.
- The built-in errors are available as definitions in `CommonErrorDefinitions`; the `Add...` methods are thin
  wrappers over them.
- Use your own code prefix (`VG` is reserved) and declare HTTP statuses in your assembly with
  `[assembly: HttpErrorCode("ACME0001", 422)]`; applications register it with `.AddAssemblyOf<AcmeErrors>()`.

### Metadata and rendering

`ValidationBuilder<T>.Create(options)` takes `ValidationOptions`:

- `NameConverter`: the naming convention of paths; `Create(NameConverters.CamelCase)` keeps the other defaults.
- `WriteValue`: a `ValueFormatter` delegate, `(object? value, string? format) => string`, writing each placeholder
  value; `ValidationOptions.DefaultWriteValue` (`FormatValue`) by default, and also when set to `null`. It
  double-quotes strings and `Guid`s (escaping `"` and `\`), so `Value "XX" for country must be one of "CH", "DE"`
  shows empty strings and tells the string `"null"` from `null`; other values use the invariant culture and the format
  specifier. With the default the writer applies these rules itself, without boxing. Set another delegate to change
  quoting or the format of any type, falling back to `ValidationOptions.FormatValue` for the rest; values are then
  boxed.
- `WriteText`: a `Func<string, string>` for text written by metadata, such as paths and entity names;
  `ValidationOptions.DefaultWriteText` (as is) by default, and also when set to `null`. The format's own text is never
  passed to it.

An entry's `metadata` is a `ValidationMetadata`, not a dictionary. A metadata type keeps its values typed, decides
how the entry's detail is rendered, and writes itself to JSON as a map:

```csharp
public abstract class ValidationMetadata
{
    protected internal virtual string RenderDetail(string format, ValidationMessageWriter writer); // default: fill placeholders
    protected internal virtual bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer);       // one placeholder
    public abstract void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options);
}
```

- Builders record entries as they are added and render them only in `ToValidationEntries()`: pointers, property
  paths (`{name}`), and every detail are produced there, from the builder hierarchy.
- An entry about a property carries it in a `PropertyValidationMetadata`, which the entry's pointer comes from. It fills
  `{name}` with the property's readable path and writes it to JSON as `name`, whether or not the format uses it.
  `validation.AddProperty(x => x.Age, "AGE001", "Age is required")` creates one for you; a custom type derives from it and
  takes the property from `validation.Resolve(x => x.Age)`:
  `validation.Add("ACME0009", "Property {name} is {age}", new AgeMetadata(validation.Resolve(x => x.Age), age))`.
- The metadata renders the detail, by default filling the format's placeholders through `TryWrite`. Override
  `RenderDetail` to render differently, for example from a localized template; it runs once per entry with
  `writer.GetClrPath(...)` available, so capture there what `WriteTo` needs later.
- An entry without a `PropertyValidationMetadata` points at its builder's scope: `validation.Add(code, "Order is closed")`.
  Without metadata the format is the detail, as is.
- In a custom type, `WriteValue(writer, key, value, options)` writes one property serialized by its static type,
  so values are not boxed.
- In `TryWrite`, `writer.WriteValue(value)` writes a value (formatted, and quoted if it is a string or `Guid`), while
  `writer.WriteText(text)` writes text as is. A value implementing `IRenderableValue` writes itself, as `ValueList<T>`
  (each item quoted) and `EntityName` (unquoted) do.
- A placeholder may carry a format specifier after `:`, applied with the invariant culture to values written with
  `writer.WriteValue`: `{value:N2}`, `{date:yyyy-MM-dd}`, `{at:HH:mm}`. `TryWrite` receives only the key; an invalid
  specifier throws `FormatException` when entries are rendered, and an unknown key is kept literally with its
  specifier.
- `ErrorDefinitionValidator.GetPlaceholders(format)` (in `ValidationGuard.CommonErrors`) returns a format's placeholder
  keys, without specifiers, read by rendering the format with the builder's own grammar (`{` up to the next `}`), for
  example to check a localized template.
- `DictionaryValidationMetadata` wraps an `IReadOnlyDictionary<string, object?>` for arbitrary values; it is also what
  deserialized `errors` entries carry, with values as `JsonElement`.
- Overrides in another assembly are declared `protected override`.

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

Endpoints return the root builder as a response with `Problem(validation)`, imported with `using static`:

```csharp
using static ValidationGuard.Http.ValidationResults;

app.MapPost("/orders", (Order order) =>
{
    using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
    // ... add entries ...
    if (validation.ToValidationEntries().Count > 0)
        return Problem(validation);
    return Results.Ok();
});
```

- `Problem(validation)` returns a `ValidationProblemResult`. It produces the entries at once, so the builder may be
  disposed before the response is written, and finds the `ValidationProblemFactory` when the response executes: no
  parameter is needed. It also documents a Problem Details response for OpenAPI. `Problem(entries)` takes entries
  already produced.
- With `builder.Services.AddProblemDetails(...)`, the response goes through ASP.NET Core's `IProblemDetailsService`, so
  `CustomizeProblemDetails` (a `traceId`, an `instance`) applies to validation problems too.
- Code that cannot return a result, such as a service, calls `validation.ThrowIfInvalid()`, which throws a
  `ValidationFailedException` with the entries when there are any. `AddValidationProblems` registers an
  `IExceptionHandler` that writes the same response; enable it with `app.UseExceptionHandler()` (which needs
  `AddProblemDetails()` when no error path is set).
- `ValidationProblemFactory.Create(entries)` and `CreateResult(entries)` remain for building the `ProblemDetails`
  yourself.

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
    - `src/ValidationGuard.CommonErrors/ValidationGuard.CommonErrors.csproj`
    - `src/ValidationGuard.CommonErrors.Http/ValidationGuard.CommonErrors.Http.csproj`
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
