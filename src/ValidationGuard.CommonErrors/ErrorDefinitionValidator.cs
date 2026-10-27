using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace ValidationGuard.CommonErrors;

/// <summary>
/// Checks <see cref="ErrorDefinition"/>s for consistency between their format and keys. Definitions are not checked
/// when they are created, so call this from a unit test:
/// <code>
/// [Fact]
/// public void ErrorDefinitions_AreValid() => ErrorDefinitionValidator.ValidateFields(typeof(AcmeErrors));
/// </code>
/// </summary>
public static class ErrorDefinitionValidator
{
    /// <summary>
    /// Throws <see cref="ArgumentException"/> unless the definition's format placeholders are exactly its built-in keys
    /// plus its declared keys (<c>{entity}</c> may be left out of a property error's format), with no key blank,
    /// duplicated, equal to a built-in key, or containing braces or ':'.
    /// Placeholders may carry format specifiers, as in <c>{value:N2}</c>; a specifier of a value whose type the
    /// definition declares (a typed property or reference error) must be valid for that type.
    /// </summary>
    public static void Validate(ErrorDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var problems = FindProblems(definition);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Error definition {definition.Code} is invalid: {string.Join(" ", problems)}", nameof(definition));
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> unless <paramref name="localizedFormat"/>, a translation of the
    /// definition's format, uses the same placeholders: every key of the definition, and no other (<c>{entity}</c> may
    /// be left out of a property error's translation). Format specifiers may differ, but must be valid for the values'
    /// types where the definition declares them.
    /// </summary>
    public static void ValidateLocalizedFormat(ErrorDefinition definition, string localizedFormat)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(localizedFormat);

        var problems = FindProblems(definition, localizedFormat);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"The translation of {definition.Code} is invalid: {string.Join(" ", problems)}",
                nameof(localizedFormat));
        }
    }

    /// <summary>
    /// Validates the translations <paramref name="localizer"/> gives, in every culture the library ships, of every
    /// public static <see cref="ErrorDefinition"/> field of <paramref name="type"/> and its nested types: the cultures of
    /// the satellite resource assemblies next to <paramref name="type"/>'s assembly (<c>de/Acme.Validation.resources.dll</c>
    /// and so on), so a language added later is validated without changing the test. Otherwise as
    /// <see cref="ValidateLocalizations(Type, IStringLocalizer, IEnumerable{CultureInfo}, bool)"/>.
    /// </summary>
    /// <returns>The number of translations found, over all cultures.</returns>
    /// <exception cref="InvalidOperationException">No satellite resource assembly was found.</exception>
    public static int ValidateLocalizations(
        Type type,
        IStringLocalizer localizer,
        bool requireAll = false)
    {
        ArgumentNullException.ThrowIfNull(type);

        var assembly = type.Assembly;
        var cultures = FindSatelliteCultures(Path.GetDirectoryName(assembly.Location), assembly.GetName().Name!);
        if (cultures.Count == 0)
        {
            throw new InvalidOperationException(
                $"No satellite resource assembly of {assembly.GetName().Name} was found, so there are no translations to validate.");
        }

        return ValidateLocalizations(type, localizer, cultures, requireAll);
    }

    /// <summary>
    /// Validates the translations <paramref name="localizer"/> gives for <paramref name="culture"/>, as
    /// <see cref="ValidateLocalizations(Type, IStringLocalizer, IEnumerable{CultureInfo}, bool)"/>.
    /// </summary>
    /// <returns>The number of definitions translated.</returns>
    public static int ValidateLocalizations(
        Type type,
        IStringLocalizer localizer,
        CultureInfo culture,
        bool requireAll = false)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return ValidateLocalizations(type, localizer, [culture], requireAll);
    }

    /// <summary>
    /// Validates the translations <paramref name="localizer"/> gives for each of <paramref name="cultures"/> (set as the
    /// current UI culture while they are looked up, as <see cref="IStringLocalizer"/> reads it) of every public static
    /// <see cref="ErrorDefinition"/> field of <paramref name="type"/> and its nested types (as
    /// <see cref="ValidateFields"/> finds them), reporting all invalid translations at once, as
    /// <see cref="ValidateLocalizedFormat"/> checks one. A library calls it from a unit test, and can require every
    /// definition to be translated. Each definition is looked up by its format, as builders look up entries.
    /// </summary>
    /// <returns>The number of translations found, over all cultures.</returns>
    /// <exception cref="ArgumentException">
    /// A translation does not use the definition's placeholders or has a specifier invalid for its value's type, or
    /// <paramref name="requireAll"/> is set and a definition is not translated.
    /// </exception>
    public static int ValidateLocalizations(
        Type type,
        IStringLocalizer localizer,
        IEnumerable<CultureInfo> cultures,
        bool requireAll = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(cultures);

        var translated = 0;
        var problems = new List<string>();
        foreach (var culture in cultures)
        {
            ArgumentNullException.ThrowIfNull(culture, nameof(cultures));
            translated += CollectLocalizationProblems(type, localizer, culture, requireAll, problems);
        }

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid translations:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}",
                nameof(localizer));
        }

        return translated;
    }

    /// <summary>
    /// The placeholder keys of <paramref name="format"/>, without format specifiers, in order of appearance: for
    /// example <c>value</c> and <c>max</c> for <c>"{value:N2} is above {max}"</c>. Use it to check a format, such as a
    /// localized template, against the keys its metadata supplies.
    /// </summary>
    public static IReadOnlyList<string> GetPlaceholders(string format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return [.. ReadPlaceholders(format).Select(placeholder => placeholder.Key)];
    }

    // The placeholders of format with their specifiers (null when none), read by rendering it, so with exactly the
    // grammar used for every entry.
    private static List<(string Key, string? Specifier)> ReadPlaceholders(string format)
    {
        var recorder = new PlaceholderRecorder();
        if (!string.IsNullOrWhiteSpace(format))
        {
            using var validation = new ValidationBuilder<object>();
            validation.Add("placeholders", format, recorder);
            validation.ToValidationEntries();
        }

        return recorder.Placeholders;
    }

    // The cultures of the satellite resource assemblies in directory: subdirectories named after a culture that hold
    // <assemblyName>.resources.dll, as the build places them; sorted by name.
    internal static List<CultureInfo> FindSatelliteCultures(string? directory, string assemblyName)
    {
        var cultures = new List<CultureInfo>();
        if (directory is null)
        {
            return cultures;
        }

        var fileName = assemblyName + ".resources.dll";
        foreach (var subdirectory in Directory.EnumerateDirectories(directory))
        {
            if (!File.Exists(Path.Combine(subdirectory, fileName)))
            {
                continue;
            }

            try
            {
                cultures.Add(CultureInfo.GetCultureInfo(Path.GetFileName(subdirectory), predefinedOnly: true));
            }
            catch (CultureNotFoundException)
            {
                // Not a culture's directory.
            }
        }

        cultures.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        return cultures;
    }

    /// <summary>
    /// Validates every public static <see cref="ErrorDefinition"/> field of <paramref name="type"/> and its nested
    /// types, reporting all invalid definitions at once. Generic holders such as
    /// <c>CommonErrorDefinitions.Comparison&lt;T&gt;</c> must be passed closed, for example
    /// <c>typeof(CommonErrorDefinitions.Comparison&lt;int&gt;)</c>; open generic nested types are skipped.
    /// </summary>
    /// <returns>The number of definitions validated.</returns>
    public static int ValidateFields(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var count = 0;
        var problems = new List<string>();
        foreach (var (field, definition) in FindDefinitions(type))
        {
            count++;
            foreach (var problem in FindProblems(definition))
            {
                problems.Add($"{field.DeclaringType!.Name}.{field.Name} ({definition.Code}): {problem}");
            }
        }

        if (count == 0)
        {
            throw new ArgumentException($"{type.Name} declares no public static error definitions.", nameof(type));
        }

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid error definitions:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}",
                nameof(type));
        }

        return count;
    }

    internal static IEnumerable<(FieldInfo Field, ErrorDefinition Definition)> FindDefinitions(Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (typeof(ErrorDefinition).IsAssignableFrom(field.FieldType) &&
                field.GetValue(null) is ErrorDefinition definition)
            {
                yield return (field, definition);
            }
        }

        foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
        {
            if (nested.ContainsGenericParameters)
            {
                continue;
            }

            foreach (var found in FindDefinitions(nested))
            {
                yield return found;
            }
        }
    }

    // Adds the problems of the translations to culture to problems, and returns how many translations were found.
    private static int CollectLocalizationProblems(
        Type type,
        IStringLocalizer localizer,
        CultureInfo culture,
        bool requireAll,
        List<string> problems)
    {
        var translations = new List<(string Name, ErrorDefinition Definition, string? Localized)>();
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            foreach (var (field, definition) in FindDefinitions(type))
            {
                translations.Add((
                    $"{field.DeclaringType!.Name}.{field.Name} ({definition.Code})",
                    definition,
                    localizer.Translate(definition.Format)));
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        var translated = 0;
        foreach (var (name, definition, localized) in translations)
        {
            if (localized is null)
            {
                if (requireAll)
                {
                    problems.Add($"{name}: not translated to {culture.Name}.");
                }

                continue;
            }

            translated++;
            foreach (var problem in FindProblems(definition, localized))
            {
                problems.Add($"{name} in {culture.Name}: {problem}");
            }
        }

        return translated;
    }

    private static List<string> FindProblems(ErrorDefinition definition) => FindProblems(definition, definition.Format);

    // The problems of definition with format in place of its own, such as a translation of it.
    private static List<string> FindProblems(ErrorDefinition definition, string format)
    {
        var problems = new List<string>();
        var builtInKeys = definition.Keys.Take(definition.BuiltInKeyCount).ToHashSet(StringComparer.Ordinal);
        var expected = new HashSet<string>(builtInKeys, StringComparer.Ordinal);

        // The built-in keys come first and are expected already; check the declared keys.
        foreach (var key in definition.Keys.Skip(definition.BuiltInKeyCount))
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                problems.Add("A key is blank.");
            }
            else if (key.AsSpan().ContainsAny('{', '}', ':'))
            {
                problems.Add($"Key '{key}' must not contain braces or ':'.");
            }
            else if (!expected.Add(key))
            {
                problems.Add(builtInKeys.Contains(key)
                    ? $"Key '{key}' is built in and must not be declared."
                    : $"Key '{key}' is declared more than once.");
            }
        }

        // The same grammar the builder renders with.
        var read = ReadPlaceholders(format);
        var placeholders = read.Select(placeholder => placeholder.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var placeholder in placeholders)
        {
            if (!expected.Contains(placeholder))
            {
                problems.Add($"Format placeholder '{{{placeholder}}}' has no matching key.");
            }
        }

        foreach (var key in expected)
        {
            var optional = key == EntityErrorDefinition.EntityKey && definition.IsEntityPlaceholderOptional;
            if (!optional && !placeholders.Contains(key))
            {
                problems.Add($"Format does not use '{{{key}}}'.");
            }
        }

        // A specifier can be checked only where the value's type is known: the typed values of a definition.
        var valueTypes = definition.ValueTypes;
        var declaredKeys = definition.Keys.Skip(definition.BuiltInKeyCount).ToList();
        foreach (var (key, specifier) in read)
        {
            var index = declaredKeys.IndexOf(key);
            if (specifier is not null && index >= 0 && index < valueTypes.Length && !IsValidSpecifier(valueTypes[index], specifier))
            {
                problems.Add($"Format specifier '{{{key}:{specifier}}}' is not valid for {valueTypes[index].Name}.");
            }
        }

        return problems;
    }

    // Whether specifier formats a value of type, tried on its default value with the rules values are written by. A
    // reference type other than a value list has no value to try, and a string ignores specifiers: both pass.
    private static bool IsValidSpecifier(Type type, string specifier)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueList<>))
        {
            // A value list applies the specifier to each item.
            return IsValidSpecifier(type.GetGenericArguments()[0], specifier);
        }

        if (!type.IsValueType)
        {
            return true;
        }

        try
        {
            ValidationOptions.FormatValue(Activator.CreateInstance(type), specifier);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Records the key and specifier of every placeholder the builder asks it to fill, and fills it with nothing.
    private sealed class PlaceholderRecorder : ValidationMetadata
    {
        public List<(string Key, string? Specifier)> Placeholders { get; } = [];

        protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
        {
            Placeholders.Add((key.ToString(), writer.FormatSpecifier));
            return true;
        }

        // Required by the base class; a recorder only renders its format and is never serialized.
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options) =>
            throw new NotSupportedException();
    }
}
