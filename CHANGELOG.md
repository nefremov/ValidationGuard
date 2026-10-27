# Changelog

All packages are versioned together and follow [Semantic Versioning](https://semver.org): from 1.0.0 on, a breaking
change to the public API (recorded in each project's `PublicAPI.Shipped.txt`) only ships in a new major version.

## [1.0.0] - 2026-10-27

The first stable release.

### Packages

| Package | Contents |
|---------|----------|
| `ValidationGuard` | The validation builder and entries: RFC 6901 JSON Pointers, nested scopes, deterministic ordering, typed metadata, options; no dependencies |
| `ValidationGuard.Localization` | Localizes messages with `IStringLocalizer` (`options.Localizers.Add(localizer)`) |
| `ValidationGuard.CommonErrors` | Ready-made errors with stable codes (`VG0001` ...): not found, already exists, null, empty, length, range, allowed values, references; `ErrorDefinition` and `ErrorDefinitionValidator` for libraries' own errors |
| `ValidationGuard.CommonErrors.Localization` | The common errors' messages translated to 47 languages (`IStringLocalizer<CommonErrorMessages>`) |
| `ValidationGuard.Http` | RFC 9457 Problem Details for ASP.NET Core: status mapping from assembly metadata, status precedence, the `errors` extension, an exception handler; bundles the `VGH001`–`VGH007` analyzer |
| `ValidationGuard.CommonErrors.Http` | HTTP statuses of the common error codes (`AddCommonErrors()`) |

### Highlights

- Entries are rendered once, when the root builder produces them (`ToValidationEntries()`), under the request's
  cultures: formats, entity and member names are translated for `CurrentUICulture`, values formatted with
  `CurrentCulture`. `ValidationOptions.UseLocalization = false` renders messages as defined, with invariant values.
- `ValidationOptions` follows the options pattern (`IOptions<ValidationOptions>`): pointer naming convention,
  localizers, and how text and values are written.
- Localization dependencies live only in the `.Localization` packages; libraries extending ValidationGuard are
  advised to split theirs the same way (see the README).

### Known limitations

- The translations of the common errors were not reviewed by native speakers yet. Placeholders and format specifiers
  are verified by tests; wording fixes will ship in patch releases.
- Acholi (`ach`) and Samoan (`sm`) are not shipped: .NET does not recognize them as cultures.
- Problem Details titles are not localized by ValidationGuard; applications translate them in
  `CustomizeProblemDetails` (see the README).
- A metadata instance belongs to one entry: create a new one for each entry.
- The packages target `net10.0` only.
