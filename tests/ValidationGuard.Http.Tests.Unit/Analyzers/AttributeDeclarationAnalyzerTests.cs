using Microsoft.CodeAnalysis;

namespace ValidationGuard.Http.Tests.Unit.Analyzers;

// [HttpErrorCode] and [HttpErrorCodeOverride] declarations within one assembly.
public class AttributeDeclarationAnalyzerTests
{
    private const string Usings = "using ValidationGuard.Http;\n";

    [Fact]
    public async Task ValidDeclarations_ReportNothing()
    {
        // Arrange
        const string source = Usings + """
            [assembly: HttpErrorCode("IDX001", 422)]
            [assembly: HttpErrorCode("IDX002", 409, Type = "https://example.test/errors/IDX002", Title = "Duplicate order")]
            [assembly: HttpErrorCode("IDX003", 403, Type = "about:blank")]
            [assembly: HttpErrorCodeOverride("IDX001", 400)]
            """;

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("""[assembly: HttpErrorCode("", 422)]""")]
    [InlineData("""[assembly: HttpErrorCode(" ", 422)]""")]
    [InlineData("""[assembly: HttpErrorCode(null, 422)]""")]
    public async Task EmptyCode_ReportsVGH001(string declaration)
    {
        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + declaration);

        // Assert
        Assert.Contains(diagnostics, d => d.Id == "VGH001");
    }

    [Theory]
    [InlineData("""[assembly: HttpErrorCode("IDX001", 200)]""")]
    [InlineData("""[assembly: HttpErrorCode("IDX001", 600)]""")]
    [InlineData("""[assembly: HttpErrorCode(status: 302, code: "IDX001")]""")]
    [InlineData("""[assembly: HttpErrorCode("IDX001", 422)] [assembly: HttpErrorCodeOverride("IDX001", 204)]""")]
    public async Task StatusOutsideErrorRange_ReportsVGH002(string declaration)
    {
        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + declaration);

        // Assert
        Assert.Equal("VGH002", Assert.Single(diagnostics).Id);
    }

    [Theory]
    [InlineData("errors/IDX001")]
    [InlineData("/errors/IDX001")]
    [InlineData("not a uri")]
    [InlineData("C:\\errors\\IDX001")]
    public async Task NonAbsoluteType_ReportsVGH003(string type)
    {
        // Arrange
        var source = Usings + $$"""
            [assembly: HttpErrorCode("IDX001", 422, Type = @"{{type}}")]
            """;

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(source);

        // Assert
        Assert.Equal("VGH003", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task DuplicateCodeInAssembly_ReportsVGH004()
    {
        // Arrange
        const string source = Usings + """
            [assembly: HttpErrorCode("IDX001", 422)]
            [assembly: HttpErrorCode("IDX001", 409)]
            """;

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH004", diagnostic.Id);
        Assert.True(diagnostic.Location.IsInSource);
    }

    [Fact]
    public async Task OverrideOfUndeclaredCode_ReportsVGH006Warning()
    {
        // Arrange
        const string source = Usings + """[assembly: HttpErrorCodeOverride("IDX404", 422)]""";

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH006", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task DuplicateOverride_ReportsVGH007()
    {
        // Arrange
        const string source = Usings + """
            [assembly: HttpErrorCode("IDX001", 422)]
            [assembly: HttpErrorCodeOverride("IDX001", 409)]
            [assembly: HttpErrorCodeOverride("IDX001", 400)]
            """;

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(source);

        // Assert
        Assert.Equal("VGH007", Assert.Single(diagnostics).Id);
    }
}
