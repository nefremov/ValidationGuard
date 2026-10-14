using Microsoft.CodeAnalysis;

namespace ValidationGuard.Http.Tests.Unit.Analyzers;

public class HttpErrorCodeAnalyzerTests
{
    private const string Usings = "using ValidationGuard.Http;\n";

    [Fact]
    public async Task ValidDeclarations_ReportNothing()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """
            [assembly: HttpErrorCode("IDX001", 422)]
            [assembly: HttpErrorCode("IDX002", 409, Type = "https://example.test/errors/IDX002", Title = "Duplicate order")]
            [assembly: HttpErrorCode("IDX003", 403, Type = "about:blank")]
            [assembly: HttpErrorCodeOverride("IDX001", 400)]
            """);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("""[assembly: HttpErrorCode("", 422)]""")]
    [InlineData("""[assembly: HttpErrorCode(" ", 422)]""")]
    [InlineData("""[assembly: HttpErrorCode(null, 422)]""")]
    public async Task EmptyCode_ReportsVGH001(string declaration)
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + declaration);

        Assert.Contains(diagnostics, d => d.Id == "VGH001");
    }

    [Theory]
    [InlineData("""[assembly: HttpErrorCode("IDX001", 200)]""")]
    [InlineData("""[assembly: HttpErrorCode("IDX001", 600)]""")]
    [InlineData("""[assembly: HttpErrorCode(status: 302, code: "IDX001")]""")]
    [InlineData("""[assembly: HttpErrorCode("IDX001", 422)] [assembly: HttpErrorCodeOverride("IDX001", 204)]""")]
    public async Task StatusOutsideErrorRange_ReportsVGH002(string declaration)
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + declaration);

        Assert.Equal("VGH002", Assert.Single(diagnostics).Id);
    }

    [Theory]
    [InlineData("errors/IDX001")]
    [InlineData("/errors/IDX001")]
    [InlineData("not a uri")]
    [InlineData("C:\\errors\\IDX001")]
    public async Task NonAbsoluteType_ReportsVGH003(string type)
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + $$"""
            [assembly: HttpErrorCode("IDX001", 422, Type = @"{{type}}")]
            """);

        Assert.Equal("VGH003", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task DuplicateCodeInAssembly_ReportsVGH004()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """
            [assembly: HttpErrorCode("IDX001", 422)]
            [assembly: HttpErrorCode("IDX001", 409)]
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH004", diagnostic.Id);
        Assert.True(diagnostic.Location.IsInSource);
    }

    [Fact]
    public async Task CodeAlsoDeclaredByReference_ReportsVGH005AtDeclaration()
    {
        var library = AnalyzerHarness.CompileLibrary("Acme.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 409)]""");

        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """[assembly: HttpErrorCode("IDX001", 422)]""", library);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH005", diagnostic.Id);
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Contains("Acme.Codes", diagnostic.GetMessage());
    }

    [Fact]
    public async Task CodeDeclaredByTwoReferences_ReportsVGH005()
    {
        var acme = AnalyzerHarness.CompileLibrary("Acme.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 409)]""");
        var contoso = AnalyzerHarness.CompileLibrary("Contoso.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 422)]""");

        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + "class App { }", acme, contoso);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH005", diagnostic.Id);
        Assert.Contains("Acme.Codes", diagnostic.GetMessage());
        Assert.Contains("Contoso.Codes", diagnostic.GetMessage());
    }

    [Fact]
    public async Task OverrideOfReferencedCode_ReportsNothing()
    {
        var library = AnalyzerHarness.CompileLibrary("Acme.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 409)]""");

        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """[assembly: HttpErrorCodeOverride("IDX001", 422)]""", library);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task OverrideOfUndeclaredCode_ReportsVGH006()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """[assembly: HttpErrorCodeOverride("IDX404", 422)]""");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH006", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task DuplicateOverride_ReportsVGH007()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """
            [assembly: HttpErrorCode("IDX001", 422)]
            [assembly: HttpErrorCodeOverride("IDX001", 409)]
            [assembly: HttpErrorCodeOverride("IDX001", 400)]
            """);

        Assert.Equal("VGH007", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task BuilderCallsWithInvalidConstants_AreReported()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """
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
            """);

        Assert.Equal(["VGH001", "VGH002", "VGH003"], diagnostics.Select(d => d.Id).Order().ToArray());
    }

    [Fact]
    public async Task BuilderCallsWithNonConstantArguments_ReportNothing()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + """
            static class Registrations
            {
                public static void Register(ErrorCodeMapBuilder builder, string code, int status, string type) =>
                    builder.Add(code, status, type);
            }
            """);

        Assert.Empty(diagnostics);
    }
}
