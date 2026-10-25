namespace ValidationGuard.Http;

public sealed record HttpErrorCode(string Code, int Status, string? Type = null, string? Title = null);
