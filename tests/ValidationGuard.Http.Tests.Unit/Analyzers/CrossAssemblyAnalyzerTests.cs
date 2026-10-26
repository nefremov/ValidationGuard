namespace ValidationGuard.Http.Tests.Unit.Analyzers;

// Declarations that collide with, or override, codes declared by referenced assemblies.
public class CrossAssemblyAnalyzerTests
{
    private const string Usings = "using ValidationGuard.Http;\n";

    [Fact]
    public async Task CodeAlsoDeclaredByReference_ReportsVGH005AtDeclaration()
    {
        // Arrange
        var library = AnalyzerHarness.CompileLibrary("Acme.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 409)]""");

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(
            Usings + """[assembly: HttpErrorCode("IDX001", 422)]""", library);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH005", diagnostic.Id);
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Contains("Acme.Codes", diagnostic.GetMessage());
    }

    [Fact]
    public async Task CodeDeclaredByTwoReferences_ReportsVGH005NamingBoth()
    {
        // Arrange
        var acme = AnalyzerHarness.CompileLibrary("Acme.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 409)]""");
        var contoso = AnalyzerHarness.CompileLibrary("Contoso.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 422)]""");

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(Usings + "class App { }", acme, contoso);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VGH005", diagnostic.Id);
        Assert.Contains("Acme.Codes", diagnostic.GetMessage());
        Assert.Contains("Contoso.Codes", diagnostic.GetMessage());
    }

    [Fact]
    public async Task OverrideOfReferencedCode_ReportsNothing()
    {
        // Arrange
        var library = AnalyzerHarness.CompileLibrary("Acme.Codes", Usings + """[assembly: HttpErrorCode("IDX001", 409)]""");

        // Act
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(
            Usings + """[assembly: HttpErrorCodeOverride("IDX001", 422)]""", library);

        // Assert
        Assert.Empty(diagnostics);
    }
}
