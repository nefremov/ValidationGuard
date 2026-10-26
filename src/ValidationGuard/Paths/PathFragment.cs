namespace ValidationGuard;

/// <summary>
/// One fragment of a property path, as resolved by <see cref="IValidationBuilder{TContext}.Resolve{TValue}"/>. Only
/// the CLR spelling is stored: member names are converted when entries are rendered.
/// </summary>
public readonly record struct PathFragment(PathFragmentKind Kind, string ClrName);
