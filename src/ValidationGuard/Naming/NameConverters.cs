namespace ValidationGuard;

/// <summary>
/// The built-in naming conventions for member names in pointers and property paths. Pass one to
/// <see cref="ValidationBuilder{TContext}.Create"/>, or implement <see cref="IPathFragmentNameConverter"/> for another.
/// </summary>
public static class NameConverters
{
    /// <summary>Member names as declared: <c>HomeAddress</c>. The default.</summary>
    public static IPathFragmentNameConverter PascalCase => PascalCaseNameConverter.Instance;

    /// <summary><c>homeAddress</c>; a leading acronym is kept: <c>URL</c>.</summary>
    public static IPathFragmentNameConverter CamelCase => CamelCaseNameConverter.Instance;

    /// <summary><c>home_address</c>.</summary>
    public static IPathFragmentNameConverter SnakeCase => SnakeCaseNameConverter.Instance;

    /// <summary><c>home-address</c>.</summary>
    public static IPathFragmentNameConverter KebabCase => KebabCaseNameConverter.Instance;

    /// <summary>The convention used when none is given: <see cref="PascalCase"/>.</summary>
    public static IPathFragmentNameConverter Default => PascalCase;
}
