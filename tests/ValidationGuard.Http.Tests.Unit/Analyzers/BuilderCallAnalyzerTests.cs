namespace ValidationGuard.Http.Tests.Unit.Analyzers;

// ErrorCodeMapBuilder.Add and Override calls in code: constant arguments are checked like attribute declarations.
public class BuilderCallAnalyzerTests
{
    private const string Usings = "using ValidationGuard.Http;\n";

    [Fact]
    public async Task CallsWithInvalidConstants_AreReported()
    {
        // Arrange
        const string source = Usings + """
            static class Registrations
            {
                public static void Register(ErrorCodeMapBuilder builder)
                {
                    builder.Add("IDX001", 200);
                    builder.Add("IDX002", 422, "relative/type");
                    builder.Override("", 422);
                    builder.Add("IDX003", 409, "https://example.test/errors/IDX003");
                    builder.Add("IDX004", 422);
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(source);

        // Assert
        Assert.Equal(["VGH001", "VGH002", "VGH003"], diagnostics.Select(d => d.Id).Order().ToArray());
    }

    [Fact]
    public async Task CallsWithNonConstantArguments_ReportNothing()
    {
        // Arrange
        const string source = Usings + """
            static class Registrations
            {
                public static void Register(ErrorCodeMapBuilder builder, string code, int status, string type) =>
                    builder.Add(code, status, type);
            }
            """;

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
