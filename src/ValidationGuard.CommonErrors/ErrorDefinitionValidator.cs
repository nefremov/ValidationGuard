using System.Reflection;
using System.Text.Json;

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
    /// Placeholders may carry format specifiers, as in <c>{value:N2}</c>.
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
    /// The placeholder keys of <paramref name="format"/>, without format specifiers, in order of appearance: for
    /// example <c>value</c> and <c>max</c> for <c>"{value:N2} is above {max}"</c>. Use it to check a format, such as a
    /// localized template, against the keys its metadata supplies.
    /// </summary>
    public static IReadOnlyList<string> GetPlaceholders(string format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (string.IsNullOrWhiteSpace(format))
        {
            return [];
        }

        // The builder renders the format, so the keys are read with exactly the grammar used for every entry.
        var recorder = new PlaceholderRecorder();
        using var validation = ValidationBuilder<object>.Create();
        validation.Add("placeholders", format, recorder);
        validation.ToValidationEntries();
        return recorder.Keys;
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

    private static List<string> FindProblems(ErrorDefinition definition)
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
        var placeholders = GetPlaceholders(definition.Format).ToHashSet(StringComparer.Ordinal);
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

        return problems;
    }

    // Records the key of every placeholder the builder asks it to fill, and fills it with nothing.
    private sealed class PlaceholderRecorder : ValidationMetadata
    {
        public List<string> Keys { get; } = [];

        protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
        {
            Keys.Add(key.ToString());
            return true;
        }

        // Required by the base class; a recorder only renders its format and is never serialized.
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options) =>
            throw new NotSupportedException();
    }
}
