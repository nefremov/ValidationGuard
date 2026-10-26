using Microsoft.Extensions.Logging.Abstractions;

namespace ValidationGuard.Http.Tests.Unit;

// A factory over a fixed map, for the ValidationProblemFactory tests.
internal static class TestFactory
{
    public const string ConflictType = "https://example.test/errors/A409";
    public const string ConflictTitle = "SKU retired";
    public const string ForbiddenType = "https://example.test/errors/A403";
    public const string ForbiddenTitle = "Channel not permitted";

    public static readonly ValidationProblemFactory Instance = new(
        new ErrorCodeMapBuilder()
            .Add("A409", 409, ConflictType, ConflictTitle)
            .Add("B409", 409)
            .Add("A403", 403, ForbiddenType, ForbiddenTitle)
            .Add("A422", 422)
            .Add("A400", 400)
            .Build(),
        StatusPrecedence.Default,
        NullLogger<ValidationProblemFactory>.Instance);

    public static ValidationEntry Entry(string pointer, string code, string detail) => new(pointer, code, detail, detail);
}
