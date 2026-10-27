namespace ValidationGuard.Http;

/// <summary>
/// Replaces, for the application, the HTTP mapping of a code another assembly registers:
/// <c>[assembly: HttpErrorCodeOverride(CommonErrorCodes.NotFound, 422)]</c>. Read by
/// <see cref="ErrorCodeMapBuilder.AddAssembly"/>, like <see cref="ErrorCodeMapBuilder.Override(string, int, string, string)"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class HttpErrorCodeOverrideAttribute : Attribute
{
    /// <summary>Maps <paramref name="code"/> to <paramref name="status"/>.</summary>
    public HttpErrorCodeOverrideAttribute(string code, int status)
    {
        Code = code;
        Status = status;
    }

    /// <summary>The validation code.</summary>
    public string Code { get; }

    /// <summary>The HTTP status.</summary>
    public int Status { get; }

    /// <summary>The problem <c>type</c> URI; the status's default when not set.</summary>
    public string? Type { get; set; }

    /// <summary>A stable, human-readable problem <c>title</c>; the status's default when not set.</summary>
    public string? Title { get; set; }
}
