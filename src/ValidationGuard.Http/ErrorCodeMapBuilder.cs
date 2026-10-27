using System.Collections.Frozen;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace ValidationGuard.Http;

/// <summary>
/// Builds an <see cref="ErrorCodeMap"/> from codes registered in code
/// (<see cref="Add(string, int, string, string)"/>) and declared by assemblies (<see cref="HttpErrorCodeAttribute"/>,
/// read by <see cref="AddAssembly"/>). A code can be registered once; an application replaces a mapping with
/// <see cref="Override(string, int, string, string)"/> or <see cref="HttpErrorCodeOverrideAttribute"/>.
/// Conflicts are reported by <see cref="Build"/> and the methods that detect them, naming where each registration
/// came from.
/// </summary>
public sealed class ErrorCodeMapBuilder
{
    private readonly Dictionary<string, Registration> _codes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Registration> _overrides = new(StringComparer.Ordinal);
    private int _fallbackStatus = StatusCodes.Status422UnprocessableEntity;
    private bool _built;

    /// <summary>
    /// Registers <paramref name="code"/> with its status and optional problem <c>type</c> and <c>title</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The code is already registered, or the map was built.</exception>
    public ErrorCodeMapBuilder Add(string code, int status, string? type = null, string? title = null) =>
        Add(code, status, type, title, source: null);

    /// <summary>
    /// Replaces the mapping of a registered <paramref name="code"/>, wherever it was registered; applied by
    /// <see cref="Build"/>, so it may come before the registration.
    /// </summary>
    /// <exception cref="InvalidOperationException">The code is already overridden, or the map was built.</exception>
    public ErrorCodeMapBuilder Override(string code, int status, string? type = null, string? title = null) =>
        Override(code, status, type, title, source: null);

    /// <summary>
    /// Registers the codes <paramref name="assembly"/> declares with <see cref="HttpErrorCodeAttribute"/>, and applies
    /// its <see cref="HttpErrorCodeOverrideAttribute"/>s.
    /// </summary>
    public ErrorCodeMapBuilder AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var source = assembly.GetName().Name;

        foreach (var attribute in assembly.GetCustomAttributes<HttpErrorCodeAttribute>())
        {
            Add(attribute.Code, attribute.Status, attribute.Type, attribute.Title, source);
        }

        foreach (var attribute in assembly.GetCustomAttributes<HttpErrorCodeOverrideAttribute>())
        {
            Override(attribute.Code, attribute.Status, attribute.Type, attribute.Title, source);
        }

        return this;
    }

    /// <summary>
    /// Registers the codes declared by the assembly of <typeparamref name="TMarker"/>, as <see cref="AddAssembly"/>.
    /// </summary>
    public ErrorCodeMapBuilder AddAssemblyOf<TMarker>() => AddAssembly(typeof(TMarker).Assembly);

    /// <summary>Sets the status of unregistered codes; 422 by default.</summary>
    public ErrorCodeMapBuilder UseFallbackStatus(int status)
    {
        EnsureNotBuilt();
        _fallbackStatus = status;
        return this;
    }

    /// <summary>Applies the overrides and builds the map; the builder cannot be used afterwards.</summary>
    /// <exception cref="InvalidOperationException">A code is overridden but not registered, or the map was built.</exception>
    public ErrorCodeMap Build()
    {
        EnsureNotBuilt();
        _built = true;

        foreach (var (code, registration) in _overrides)
        {
            if (!_codes.ContainsKey(code))
            {
                throw new InvalidOperationException(
                    $"HTTP error code '{code}' is overridden {DescribeSource(registration.Source)} but is not registered. " +
                    "Override can only replace a code added by Add or AddAssembly.");
            }

            _codes[code] = registration;
        }

        return new ErrorCodeMap(
            _codes.ToFrozenDictionary(x => x.Key, x => x.Value.ErrorCode, StringComparer.Ordinal),
            _fallbackStatus);
    }

    private ErrorCodeMapBuilder Add(string code, int status, string? type, string? title, string? source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        EnsureNotBuilt();

        if (_codes.TryGetValue(code, out var existing))
        {
            throw new InvalidOperationException(
                $"HTTP error code '{code}' is registered {DescribeSource(existing.Source)} and again {DescribeSource(source)}. " +
                "Use Override to replace an existing mapping.");
        }

        _codes.Add(code, new Registration(new HttpErrorCode(code, status, type, title), source));
        return this;
    }

    private ErrorCodeMapBuilder Override(string code, int status, string? type, string? title, string? source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        EnsureNotBuilt();

        if (_overrides.TryGetValue(code, out var existing))
        {
            throw new InvalidOperationException(
                $"HTTP error code '{code}' is overridden {DescribeSource(existing.Source)} and again {DescribeSource(source)}. " +
                "A code can be overridden only once.");
        }

        _overrides.Add(code, new Registration(new HttpErrorCode(code, status, type, title), source));
        return this;
    }

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("The error code map has already been built.");
        }
    }

    private static string DescribeSource(string? source) =>
        source is null ? "in code" : $"by assembly '{source}'";

    private sealed record Registration(HttpErrorCode ErrorCode, string? Source);
}
