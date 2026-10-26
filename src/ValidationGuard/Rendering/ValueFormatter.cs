namespace ValidationGuard;

/// <summary>
/// Formats a placeholder value for a detail: the value (boxed if it is a value type) and the placeholder's format
/// specifier, such as <c>N2</c> in <c>{value:N2}</c>, or <see langword="null"/> when there is none.
/// </summary>
public delegate string ValueFormatter(object? value, string? format);
