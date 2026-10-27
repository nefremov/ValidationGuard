# Samples

The samples reference the library projects in this repository; outside it, reference the NuGet packages named in each
project file. They are part of `ValidationGuard.slnx`, so they are built and tested with the rest of the solution.

| Project | Shows |
|---------|-------|
| `ValidationGuard.Samples.Console` | The builder on its own: common errors, nested builders, dictionary keys, an application's own codes, an entry without a property, and each entry's pointer, detail and metadata |
| `ValidationGuard.Samples.WebApi` | A minimal API answering with RFC 9457 Problem Details: registering status mappings, choosing the status from the entries, the `errors` extension, messages localized per request culture through `IStringLocalizer`, `ValidationOptions` configured with the options pattern |
| `Acme.Validation` | A library extending ValidationGuard: `ErrorDefinition`s (including one generic over the value type), typed `Add...` helpers, and a custom `PropertyValidationMetadata` type using format specifiers; it depends on `ValidationGuard.CommonErrors` only |
| `Acme.Validation.Localization` | The extension's German translations in `.resx` resources for `IStringLocalizer<AcmeMessages>`, a package of their own |
| `Acme.Validation.Http` | The extension's HTTP statuses declared with `[assembly: HttpErrorCode]`, checked by the analyzer, and an `AddAcmeErrors()` registration helper |
| `Acme.Validation.Tests` | The tests an extension needs: `ErrorDefinitionValidator.ValidateFields` over its definitions, a status for every code, rendering, and `ValidateLocalizations` over its translations |

Run them with:

```bash
dotnet run --project samples/ValidationGuard.Samples.Console
```

```bash
dotnet run --project samples/ValidationGuard.Samples.WebApi
```

The web sample listens on `http://localhost:5080`; `ValidationGuard.Samples.WebApi/Orders.http` has requests for a
valid order (201), a single error with its declared `type` and `title` (409), and several errors (404 wins by
precedence; every entry is listed), and the same errors in German (`?culture=de`, or `Accept-Language: de`).

`Acme.Validation` follows [Localizing a library that extends ValidationGuard](../README.md#localizing-a-library-that-extends-validationguard):
its translations ship in `Acme.Validation.Localization`: `AcmeMessages` with `[assembly: ResourceLocation("Resources")]`,
`[assembly: RootNamespace("Acme.Validation")]` and `Resources/AcmeMessages.de.resx`, checked by
`AcmeLocalizationTests` with the real resource factory. The web sample registers it after its own `AppMessages`.
