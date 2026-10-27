namespace ValidationGuard.Http;

/// <summary>The HTTP mapping of a validation code.</summary>
/// <param name="Code">The validation code, such as <c>VG0001</c>.</param>
/// <param name="Status">The HTTP status of a problem whose leading entry has the code.</param>
/// <param name="Type">The problem <c>type</c> URI, or <see langword="null"/> for the status's default.</param>
/// <param name="Title">A stable, human-readable problem <c>title</c>, or <see langword="null"/> for the status's default.</param>
public sealed record HttpErrorCode(string Code, int Status, string? Type = null, string? Title = null);
