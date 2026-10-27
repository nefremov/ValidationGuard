namespace ValidationGuard.Http;

/// <summary>
/// Declares the HTTP mapping of a validation code defined by the assembly, read by
/// <see cref="ErrorCodeMapBuilder.AddAssembly"/>: <c>[assembly: HttpErrorCode("ACME0001", 422)]</c>. The
/// ValidationGuard.Http analyzer checks the declarations at compile time.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class HttpErrorCodeAttribute : Attribute
{
    /// <summary>Maps <paramref name="code"/> to <paramref name="status"/>.</summary>
    public HttpErrorCodeAttribute(string code, int status)
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
