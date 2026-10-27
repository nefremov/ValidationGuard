# ValidationGuard

Typed validation error accumulation for .NET 10 with RFC 6901 JSON Pointer paths and deterministic output ordering.

## Packages

| Package | Purpose |
|---------|---------|
| `ValidationGuard` | Transport-agnostic validation builder and entries |
| `ValidationGuard.CommonErrors` | Ready-made errors with stable codes and message formats (not found, null, empty, range, length), translated to 47 languages |
| `ValidationGuard.Http` | RFC 9457 Problem Details projection for ASP.NET Core, with a bundled build-time analyzer |
| `ValidationGuard.CommonErrors.Http` | HTTP statuses for the `ValidationGuard.CommonErrors` codes |

Runnable examples, including a library that extends ValidationGuard with its own errors, are in [samples](samples/README.md).

## ASP.NET Core setup

The four common setups, from the simplest. Each builds on the previous one; the
[web sample](samples/ValidationGuard.Samples.WebApi/Program.cs) shows the last one in full.

### Without localization

Reference `ValidationGuard.CommonErrors.Http` (it brings `ValidationGuard`, `ValidationGuard.CommonErrors` and
`ValidationGuard.Http`), then register the HTTP statuses of every code the application reports and the options of its
builders:

```csharp
using Microsoft.Extensions.Options;
using ValidationGuard;
using ValidationGuard.CommonErrors;
using ValidationGuard.CommonErrors.Http;
using ValidationGuard.Http;
using static ValidationGuard.Http.ValidationResults;

[assembly: HttpErrorCode("APP0001", 422, Title = "Duplicate SKU")]   // the application's own codes, if any

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidationProblems(codes => codes
    .AddCommonErrors()                 // VG0001 ... from ValidationGuard.CommonErrors.Http
    .AddAssemblyOf<Program>());        // the application's own [assembly: HttpErrorCode]
builder.Services.Configure<ValidationOptions>(options => options.NameConverter = NameConverters.CamelCase);
// ASP.NET Core's Problem Details service: needed by the exception handler below, and the place to enrich every
// Problem Details response, validation problems included.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    // The request's trace identifier lets a client report an error that support can find in the server logs.
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);

var app = builder.Build();
app.UseExceptionHandler();             // writes ValidationFailedException thrown by ThrowIfInvalid

app.MapPost("/orders", (CreateOrder order, IOptions<ValidationOptions> options) =>
{
    using var validation = new ValidationBuilder<CreateOrder>(options.Value);
    if (string.IsNullOrEmpty(order.Email))
    {
        validation.AddEmpty("Order", x => x.Email);
    }

    return validation.Valid ? Results.Created() : Problem(validation);
});

app.Run();
```

Code below the endpoints that cannot return a result injects `IOptions<ValidationOptions>` the same way and calls
`validation.ThrowIfInvalid()`; the exception handler writes the same Problem Details response.

### With localization

Messages follow the request's culture once request localization is enabled and the builders' options list the
localizers. `ValidationGuard.CommonErrors` ships the common errors translated to 47 languages
(`IStringLocalizer<CommonErrorMessages>`); the application translates its own formats, its entity and member names,
and (if it wants) the Problem Details titles, in resources keyed by the English text:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddOptions<ValidationOptions>()
    .Configure<IStringLocalizer<AppMessages>, IStringLocalizer<CommonErrorMessages>>((options, app, common) =>
    {
        options.NameConverter = NameConverters.CamelCase;
        options.Localizers.Add(app);      // first: the application can override any library translation
        options.Localizers.Add(common);
    });
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // The request's trace identifier lets a client report an error that support can find in the server logs.
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

    // ValidationGuard localizes detail and errors; the title is the application's to translate.
    var titles = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<AppMessages>>();
    if (context.ProblemDetails.Title is { } title && titles.Translate(title) is { } localized)
    {
        context.ProblemDetails.Title = localized;
    }
});

var app = builder.Build();

// The languages the application answers in. CommonErrorMessages.GetSupportedCultures() lists those of the common
// errors, English (their NeutralCulture) included: support them all, or only those the application also translates
// its own messages to.
string[] cultures = [.. CommonErrorMessages.GetSupportedCultures().Intersect(["en", "de", "fr", "ja"])];
app.UseRequestLocalization(options => options
    .SetDefaultCulture(CommonErrorMessages.NeutralCulture)
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));
app.UseExceptionHandler();

internal sealed class AppMessages;   // names Resources/AppMessages.*.resx
```

- `AddLocalization` registers ASP.NET Core's resource-based localization: `IStringLocalizer<T>` for any `T`, reading
  the `.resx` resources that `T` names (`AppMessages` → `AppMessages.de.resx`, `AppMessages.fr.resx` ...) from their
  satellite assemblies. `ResourcesPath` is the folder of the application's own resources (`Resources/AppMessages.de.resx`);
  without it they sit next to the class. Libraries declare their own folder with `[ResourceLocation]`, so this setting
  does not affect them.
- `AppMessages` is an empty class that only names the application's resources; any type works, one per set of
  resources.
- `CurrentCulture` formats the values and `CurrentUICulture` selects the translations; `UseRequestLocalization` sets
  both per request, from the query string (`?culture=de`), a cookie or the `Accept-Language` header. Pointers never
  change. See [Localization](#localization) for the rules.

### With third-party libraries

A library extending ValidationGuard (like the `Acme.Validation` sample) brings its own errors and, in a companion
package, their HTTP statuses. Reference both and register its statuses next to the others:

```csharp
using Acme.Validation;        // AddSkuRetired, AddNotBetween ...
using Acme.Validation.Http;   // AddAcmeErrors

builder.Services.AddValidationProblems(codes => codes
    .AddCommonErrors()
    .AddAcmeErrors()          // or .AddAssemblyOf<AcmeHttp>(): the assembly declaring the statuses
    .AddAssemblyOf<Program>()
    .Override(AcmeErrorCodes.SkuRetired, 422));   // optional: the application decides
```

Its errors are added like the common ones (`validation.AddSkuRetired(x => x.Sku, sku)`). A code registered by two
libraries fails at startup (and the bundled analyzer warns at build time); override a status in the application, not by
registering a code again.

### With third-party libraries and localization

Add each library's localizer to the chain, after the application's own and before the common errors', so the
application can override any of them:

```csharp
builder.Services.AddOptions<ValidationOptions>()
    .Configure<IStringLocalizer<AppMessages>, IStringLocalizer<AcmeMessages>, IStringLocalizer<CommonErrorMessages>>(
        (options, app, acme, common) =>
        {
            options.NameConverter = NameConverters.CamelCase;
            options.Localizers.Add(app);
            options.Localizers.Add(acme);
            options.Localizers.Add(common);
        });
```

A library's translations cover its own formats and entity names in the languages it ships; for other languages its
messages stay in English unless the application translates them in its own resources (keyed by the library's English
formats). The application also translates its member names, which no library knows.

## Common errors with ValidationGuard.CommonErrors

Extension methods on any validation builder add frequently needed errors with a stable code, a localizable
`format`, a rendered `detail`, and one `metadata` value per format placeholder. They only report: the caller decides
whether a value is invalid.

```csharp
using ValidationGuard.CommonErrors;

var options = new ValidationOptions { NameConverter = NameConverters.CamelCase }; // or IOptions<ValidationOptions>.Value
using var validation = new ValidationBuilder<CreateOrder>(options);

if (!customers.Exists(request.CustomerId))
    validation.AddNotFound("Customer", x => x.CustomerId, request.CustomerId);

if (request.Quantity < 1)
    validation.AddLessThan("Order", x => x.Quantity, request.Quantity, 1);
```

produces

```json
{ "pointer": "", "code": "VG0001",
  "format": "{entity} with {name} = {value} does not exist",
  "detail": "Customer with CustomerId = \"3f2b...e1\" does not exist",
  "metadata": { "entity": "Customer", "name": "CustomerId", "value": "3f2b...e1" } }
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
  `OrderLine with OrderId = "3f2b...e1" and LineId = "9c1d...07" does not exist`;
  `AddNotUnique("Product", x => x.Sku, sku, x => x.VendorId, vendorId)` produces
  `Product with Sku = "CH-12" and VendorId = "3f2b...e1" already exists`. The key properties are symmetric: the entry points
  at the builder's scope (the entity), and every property's path and value appear in the message and metadata.
  Both "already exists" messages read alike, but the codes differ so clients can tell them apart:
  `AddAlreadyExists` reports a primary-key conflict (`VG0010`), `AddNotUnique` a unique-constraint
  conflict (`VG0011`).
- Every error is about an entity, passed first: property errors too, e.g. `AddNull("Customer", x => x.Email)`. The
  entity is always in `metadata` (`"entity": "Customer"`); the built-in property formats do not show it.
- `{name}` is the property's path from the root with member names as declared in C#, e.g. `CustomerId`,
  `Address.City`, `Lines[2].Sku` or `Tags["a/b"]`, or as translated (see [Localization](#localization)); it includes
  the scope of nested builders. The naming convention (`NameConverter`) applies to pointers only: `/lines/2/sku`.
- Range methods take the offending value and the bound, e.g. `AddLessThan("Order", x => x.Quantity, request.Quantity, 1)`;
  both appear in `detail` and `metadata`. Values in `detail` are formatted with the current culture (see
  [Localization](#localization)).
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
- The formats are translated to 47 languages (Arabic to Vietnamese, Simplified and Traditional Chinese), shipped as
  resources for `IStringLocalizer<CommonErrorMessages>`; add that localizer to `ValidationOptions.Localizers` (see
  [With localization](#with-localization)). `CommonErrorMessages.GetSupportedCultures()` lists the cultures the
  messages are available in, `CommonErrorMessages.NeutralCulture` (`en`, the formats' own) included, to compute the
  cultures an application supports. Other languages, and English, keep the formats as defined; an application
  overrides any translation in its own resources, keyed by the English format.
- The package depends only on `ValidationGuard` and `Microsoft.Extensions.Localization`. The HTTP statuses above are provided by
  `ValidationGuard.CommonErrors.Http`; register them with
  `AddValidationProblems(codes => codes.AddCommonErrors())` and change one with
  `.Override(CommonErrorCodes.NotFound, 422)` or `[assembly: HttpErrorCodeOverride(...)]`.

### Defining your own errors

Libraries can define errors with the same conventions as the built-in ones: `{name}` as the property path, typed
rendering, and one metadata value per placeholder. Define each error once as an
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
// "Value 12 for Age must be between 18 and 65"   metadata: entity, name, value, min, max
validation.Add(AcmeErrors.SlugTaken, "Article", x => x.Slug, request.Slug);
// "Article with Slug = \"hello-world\" is taken"             metadata: entity, name, value
```

| Type | Built-in placeholders | Other placeholders |
|------|-----------------------|--------------------|
| `EntityErrorDefinition` (1, 2 or 3 key properties) | `{entity}`, passed to `Add` | a `{name}`/`{value}` pair per key property (`{name1}`, `{value1}` ... for 2 or 3), typed by its accessor |
| `PropertyErrorDefinition`, `PropertyErrorDefinition<T1>` ... `<T1, T2, T3>` (derive from `EntityErrorDefinition`) | `{entity}`, passed to `Add`, optional in the format; `{name}`, from the accessor | 0-3 typed arguments with declared keys |
| `ReferenceErrorDefinition<T>` (derives from `EntityErrorDefinition`) | as a property error, plus `{referenced_entity}`, the referenced entity's name passed to `Add` | the key value of type `T` (`{value}`, or the key passed to the constructor), which may differ from the property's type (`int` for an `int?` property) |

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
- Use your own code prefix (`VG` is reserved) and declare HTTP statuses with
  `[assembly: HttpErrorCode("ACME0001", 422)]`, best in a companion package so the library stays free of ASP.NET Core
  (see [Writing a library that extends ValidationGuard](#writing-a-library-that-extends-validationguard)).

### Metadata and rendering

`new ValidationBuilder<T>(options)` takes `ValidationOptions`; `new ValidationBuilder<T>()` uses the defaults. It is a
plain options class, so in an application configure it once with the options pattern and inject
`IOptions<ValidationOptions>`:

```csharp
builder.Services.Configure<ValidationOptions>(options => options.NameConverter = NameConverters.CamelCase);

app.MapPost("/orders", (CreateOrder order, IOptions<ValidationOptions> options) =>
{
    using var validation = new ValidationBuilder<CreateOrder>(options.Value);
    // ...
});
```

- `NameConverter`: the naming convention of member names in JSON pointers (`/customerId`); readable paths keep the
  declared names (`CustomerId`). PascalCase by default.
- `WriteValue`: a `Func<object?, string?, string>`, `(value, format) => text`, writing each placeholder
  value; `ValidationOptions.DefaultWriteValue` (`FormatValue`) by default, and also when set to `null`. It
  double-quotes strings and `Guid`s (escaping `"` and `\`), so `Value "XX" for Country must be one of "CH", "DE"`
  shows empty strings and tells the string `"null"` from `null`; other values use the current culture and the format
  specifier. With the default the writer applies these rules itself, without boxing. Set another delegate to change
  quoting or the format of any type, falling back to `ValidationOptions.FormatValue` for the rest; values are then
  boxed.
- `WriteText`: a `Func<string, string>`, `text => text`, for text written by metadata, such as paths and
  entity names; `ValidationOptions.DefaultWriteText` (as is) by default, and also when set to `null`. The format's own
  text is never passed to it.
- `Localizers`: the `IStringLocalizer`s translating formats, entity names and member names (see
  [Localization](#localization)); none by default.

An entry's `metadata` is a `ValidationMetadata`, not a dictionary. A metadata type keeps its values typed, decides
how the entry's detail is rendered, and writes itself to JSON as a map. A metadata instance belongs to one entry: it
keeps what it captures while rendering (the property path, localized names) for its JSON, so create a new one for each
`Add`; an instance shared by two entries writes the last rendered entry's state for both.

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
- `ToValidationEntries()` completes the builder: each entry is rendered once, later calls return the same entries,
  and adding an entry or a nested builder afterwards throws `InvalidOperationException`. This holds even if rendering
  fails (a custom metadata type or `WriteText` throwing): later calls rethrow that exception instead of rendering
  again. To check for errors without completing, use `validation.Valid` (no entry in the builder or its nested
  builders); `ThrowIfInvalid()` completes the builder only when it throws.
- Entries are ordered by pointer, code, then format as added (before translation), so the order is the same in every
  culture; entries equal in all three keep the order they were added in.
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
  (each item quoted) does.
- Names are text, values are values: entity names (`{entity}`, `{referenced_entity}`) and property paths (`{name}`,
  `{name1}` ...) are written with `WriteText`, unquoted; argument values with `WriteValue`, so a string argument is
  quoted. Write a name with `WriteText` in custom metadata too.
- A placeholder may carry a format specifier after `:`, applied with the current culture to values written with
  `writer.WriteValue`: `{value:N2}`, `{date:yyyy-MM-dd}`, `{at:HH:mm}`. `TryWrite` receives only the key, and
  `writer.FormatSpecifier` the specifier, for metadata writing a value its own way. A specifier that is not valid for
  the value (`{value:Q}`) is ignored, and the value written as without one; an unknown key is kept literally with its
  specifier.
- `ErrorDefinitionValidator.GetPlaceholders(format)` (in `ValidationGuard.CommonErrors`) returns a format's placeholder
  keys, without specifiers, read by rendering the format with the builder's own grammar (`{` up to the next `}`), for
  example to check a localized template.
- `DictionaryValidationMetadata` wraps an `IReadOnlyDictionary<string, object?>` for arbitrary values; it is also what
  deserialized `errors` entries carry, with values as `JsonElement`.
- Overrides in another assembly are declared `protected override`.

### Localization

Messages are localized with the standard `IStringLocalizer` (`Microsoft.Extensions.Localization`) when entries are
rendered (`ToValidationEntries()`), under the ambient cultures: `CultureInfo.CurrentCulture` formats values, and
`ValidationOptions.Localizers` translate for `CultureInfo.CurrentUICulture`. In ASP.NET Core, request localization
sets both per request, and the localizers come from dependency injection:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddOptions<ValidationOptions>()
    .Configure<IStringLocalizer<AppMessages>, IStringLocalizer<AcmeMessages>>((options, app, acme) =>
    {
        options.Localizers.Add(app);   // asked first: the first translation found wins
        options.Localizers.Add(acme);
    });

app.UseRequestLocalization(options => options
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "de")
    .AddSupportedUICultures("en", "de"));
```

- The entry's `format` is the translation, and its `detail` is rendered from it: `Wert 0,5 für Preis muss mindestens
  1,25 sein`. Without a translation the defined format is kept, but values still follow the culture.
- Entity names (`{entity}`, `{referenced_entity}`) and the member names of readable paths (`{name}`) are translated in
  the detail and in the serialized metadata. Pointers are never localized: `/lines/0/quantity` is the same in every
  culture.
- Values are formatted with the culture, including format specifiers: `{amount:N2}` renders `1.234,50` in German.
  Outside a request (a worker, a test) set `CultureInfo.CurrentCulture` and `CurrentUICulture`, or
  `CultureInfo.InvariantCulture` for culture-independent details.

Resource keys:

| Translated | Key | Example |
|------------|-----|---------|
| A format | the format as defined, in English | `Value for {name} is required` |
| An entity name | the name | `Product` |
| A member name in a path | the CLR member name | `Sku` (the translation is written as is, without `NameConverter`) |

- A format is its own key, so every format gets its own translation: the 1-, 2- and 3-key variants of `NotFound`, the
  four `OutOfRange` variants, and different ad-hoc formats added under one code. The built-in formats are the
  constants of `CommonErrorFormats`.
- Rewording a format changes its key, so its translations must be re-keyed; `ValidateLocalizations` with `requireAll`
  reports the ones left behind.
- An entity and a member of the same name share a translation.
- `localizer.Translate(key)` performs these lookups, returning `null` when the resource is not found.
- Localization never fails a response: a localizer that throws leaves the text untranslated, and a translated
  specifier that is not valid for the value is ignored.
- A localized format may use specifiers and reorder placeholders, but must use the same keys.
  `ErrorDefinitionValidator.ValidateLocalizedFormat(definition, translation)` checks one translation;
  `ValidateLocalizations` checks a whole set (see below). Specifiers are checked where the value's type is known (the
  typed values of property and reference definitions); an entity error's key values are typed only where it is added.
- In a custom metadata type, `writer.Localize(name)` returns an entity name's translation (or the name itself), and
  `writer.Culture` the culture values are formatted with.

#### Localizing a library that extends ValidationGuard

A library with its own errors (like the `Acme.Validation` sample) ships their translations, and applications plug
them in next to their own. The library translates what it owns: the formats of its codes and the entity names it
passes to `Add` (such as `Product`). Applications translate their own formats and their own entity and member names;
the common errors come translated in `ValidationGuard.CommonErrors`.

**What to ship.** Resources for `IStringLocalizer<T>`: a marker type naming them, `.resx` files, and the resource
location, so the lookup does not depend on the `ResourcesPath` the application configures for its own resources:

```text
Acme.Validation/
  AcmeMessages.cs                  marker type + [assembly: ResourceLocation("Resources")] + [assembly: NeutralResourcesLanguage("en")]
  Resources/AcmeMessages.resx      neutral: empty, so cultures without a translation keep the defined (English) formats
  Resources/AcmeMessages.de.resx   German: formats keyed by their English text, Product by name
```

```csharp
using System.Resources;
using Microsoft.Extensions.Localization;

[assembly: ResourceLocation("Resources")]
[assembly: NeutralResourcesLanguage("en")]   // the formats' own language: no English satellite is looked up

namespace Acme.Validation;

/// <summary>The resources of Acme's messages, for IStringLocalizer&lt;AcmeMessages&gt;.</summary>
public sealed class AcmeMessages;
```

- Reference `Microsoft.Extensions.Localization` for `[ResourceLocation]`; nothing else is needed at run time. The
  `.resx` files compile into satellite assemblies (`de/Acme.Validation.resources.dll`) that `dotnet pack` includes in
  the package.
- Without `[ResourceLocation]`, the resources are looked up under the application's `ResourcesPath` inside the
  library's assembly, and are found only if the folder names happen to match.
- `[NeutralResourcesLanguage("en")]` states the language of the formats as defined: English requests are served from
  the neutral resources without looking for an English satellite. Applications can read it to list the cultures the
  library's messages are available in, as `CommonErrorMessages.NeutralCulture` and `GetSupportedCultures()` do for the
  common errors.
- The marker type's namespace and the folder make the resource name (`Acme.Validation.Resources.AcmeMessages`). If
  the library's root namespace differs from its assembly name, also add `[assembly: RootNamespace("...")]`.

**Keys.** Formats are keyed by their English text, exactly as defined (`Value {value} for {name} must be between {min}
and {max}`), so each definition has its own translation.

- Rewording a format in a new version changes its key: re-key its translations in the same change. The translation
  test below (with `requireAll`) fails until every definition is translated again.
- Entity names are keyed by the name as passed to `Add` (`Product`). Member names are keyed by the CLR member name;
  a library translates them only for model types of its own (they are written into paths as is, like untranslated
  names).

**Custom metadata.** Formats are translated before the metadata renders them, so a custom metadata type needs no
localization code of its own except for names: write entity names as `writer.WriteText(writer.Localize(name))`,
store the localized name if `WriteTo` writes it to JSON, and write values with `writer.WriteValue` so they follow the
culture. `{name}` (`PropertyValidationMetadata.Name`) is already localized.

**Test every translation** with the localizer an application would get, built from the real resource factory; giving
it a `ResourcesPath` of its own also proves `[ResourceLocation]`:

```csharp
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

private static readonly IStringLocalizer Localizer = new StringLocalizer<AcmeMessages>(
    new ResourceManagerStringLocalizerFactory(
        Options.Create(new LocalizationOptions { ResourcesPath = "Translations" }), NullLoggerFactory.Instance));

[Fact]
public void Translations_AreValid() => ErrorDefinitionValidator.ValidateLocalizations(
    typeof(AcmeErrors), Localizer, requireAll: true);
```

Without a culture, `ValidateLocalizations` validates every language the library ships: the cultures of the satellite
resource assemblies next to the type's assembly (`de/Acme.Validation.resources.dll` ...), so a language added later is
checked without changing the test; it throws when it finds none. Pass a culture, or a list of them, to check those
only. For each culture (set as the current UI culture meanwhile) it looks up every public static `ErrorDefinition` of
the type and its nested types, by its format. It returns the number of
translations found and reports, all at once, every translation whose placeholders differ from its definition's or
whose specifier is not valid for a value of known type, and with `requireAll` every missing one. Call it once per
holder type, including generic holders closed over a type (`typeof(AcmeErrors.Range<int>)`). Also render an entry or
two with `CultureInfo.CurrentCulture` and `CurrentUICulture` set to a culture, to check the result reads well.

**How applications register it.** With `AddLocalization()`, `IStringLocalizer<AcmeMessages>` comes from dependency
injection; the application adds it after its own localizer, so the application can override any library translation:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddOptions<ValidationOptions>()
    .Configure<IStringLocalizer<AppMessages>, IStringLocalizer<AcmeMessages>>((options, app, acme) =>
    {
        options.Localizers.Add(app);   // first: overrides Acme's translations
        options.Localizers.Add(acme);
    });
```

Outside dependency injection, create it as in the test above, or with any other `IStringLocalizer` implementation.

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

app.MapPost("/orders", (Order order, IOptions<ValidationOptions> options) =>
{
    using var validation = new ValidationBuilder<Order>(options.Value);
    // ... add entries ...
    if (!validation.Valid)
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
- ValidationGuard localizes `detail` and `errors`, not `title`: neither the declared titles (`Title = "Duplicate SKU"`)
  nor ASP.NET Core's default status titles (`"Not Found"`) are translated. Translate them in `CustomizeProblemDetails`,
  which runs under the request's culture with the title already set, for example keyed by the English title in the
  application's resources:

  ```csharp
  builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
  {
      var titles = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<AppMessages>>();
      if (context.ProblemDetails.Title is { } title && titles.Translate(title) is { } localized)
      {
          context.ProblemDetails.Title = localized;
      }
  });
  ```
- Code that cannot return a result, such as a service, calls `validation.ThrowIfInvalid()`, which throws a
  `ValidationFailedException` with the entries when there are any, and otherwise leaves the builder open for further
  checks. `AddValidationProblems` registers an
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

## Writing a library that extends ValidationGuard

A library can add its own errors with the same conventions as the built-in ones, so applications use them alike. The
`Acme.Validation`, `Acme.Validation.Http` and `Acme.Validation.Tests` samples are a complete example.

1. **Packages.** The library (`Acme.Validation`) references `ValidationGuard.CommonErrors` (for `ErrorDefinition` and
   the validator) and `Microsoft.Extensions.Localization` (for `[ResourceLocation]`). Its HTTP statuses go in a
   companion package (`Acme.Validation.Http`) referencing `ValidationGuard.Http`, so the library itself stays free of
   ASP.NET Core.
2. **Codes.** Choose a prefix of your own (`VG` is reserved) and keep the codes in a constants class
   (`AcmeErrorCodes`). Never renumber or reuse a code: clients identify failures by it.
3. **Errors.** Define each error once as an `ErrorDefinition` (see [Defining your own errors](#defining-your-own-errors))
   and offer typed `Add...` extension methods over `IValidationBuilder<TContext>`, as `AcmeValidationExtensions` does.
   Formats state what is wrong (`Value {value} for {name} must be between {min} and {max}`), use `{name} = {value}` for
   key properties, and are English.
4. **Custom metadata** (only for what a definition cannot express): derive from any `ValidationMetadata`. Derive
   from `PropertyValidationMetadata` when the error is about one property: the entry then points at that property and
   `{name}` is its path, both provided by the base class. Derive from `ValidationMetadata` itself for an error about
   the builder's scope (the entry points at the scope). Write names with `WriteText` (entity names through
   `writer.Localize`) and values with `WriteValue`, and create a new instance for each entry (see
   [Metadata and rendering](#metadata-and-rendering)).
5. **HTTP statuses.** In the companion package, declare every code with `[assembly: HttpErrorCode(code, status)]`
   (optionally a `Type` and a stable `Title`), and offer a registration helper that reads them:

   ```csharp
   [assembly: HttpErrorCode(AcmeErrorCodes.SkuRetired, 409, Title = "SKU retired")]

   public static class AcmeHttp
   {
       public static ErrorCodeMapBuilder AddAcmeErrors(this ErrorCodeMapBuilder builder) =>
           builder.AddAssembly(typeof(AcmeHttp).Assembly);
   }
   ```

   The analyzer bundled with `ValidationGuard.Http` checks the declarations at build time. The attributes are optional:
   the helper may instead register the codes itself, which suits statuses computed in code. The analyzer then checks the
   constant arguments (blank codes, statuses, `type` URIs), but a code registered twice is reported only at startup:

   ```csharp
   public static ErrorCodeMapBuilder AddAcmeErrors(this ErrorCodeMapBuilder builder) => builder
       .Add(AcmeErrorCodes.NotBetween, 422)
       .Add(AcmeErrorCodes.SkuRetired, 409, title: "SKU retired");
   ```

   Either way, applications call `.AddAcmeErrors()`; with attributes they may also register the declaring assembly
   directly (`.AddAssemblyOf<AcmeHttp>()`).
6. **Localization.** Ship translations of your formats and entity names as `.resx` resources for
   `IStringLocalizer<T>`, keyed by the English text, with a marker type, `[assembly: ResourceLocation("Resources")]`
   and `[assembly: NeutralResourcesLanguage("en")]`;
   see [Localizing a library that extends ValidationGuard](#localizing-a-library-that-extends-validationguard). Keep
   them in step with the formats: rewording a format changes its key.
7. **Tests.** Cover what applications rely on: every definition is valid
   (`ErrorDefinitionValidator.ValidateFields(typeof(AcmeErrors))`, also for generic holders closed over a type), every
   code has an HTTP status, the messages render as intended, and every shipped language translates every definition
   (`ErrorDefinitionValidator.ValidateLocalizations(typeof(AcmeErrors), localizer, requireAll: true)`).
8. **Document the setup** for applications: the packages to reference, `.AddAcmeErrors()`, and the localizer to add
   (`IStringLocalizer<AcmeMessages>`), as in [With third-party libraries and localization](#with-third-party-libraries-and-localization).

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
